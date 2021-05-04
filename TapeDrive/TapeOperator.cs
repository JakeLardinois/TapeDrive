using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using System.IO;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

/*Taken from:
 * http://www.codeproject.com/Articles/15487/Magnetic-Tape-Data-Storage-Part-1-Tape-Drive-IO-Co
 * http://www.codeproject.com/Articles/122769/Magnetic-Tape-Data-Storage-Part-2-Media-Changer-St
 * Resource Documenting windows API Calls (use for examples of existing calls):
 * http://msdn.microsoft.com/en-us/library/windows/desktop/aa362532(v=vs.85).aspx
 */
namespace TapeDrive
{
    #region Typedefenitions
    using BOOL = System.Int32;
    #endregion

    /// <summary>
    /// Low level Tape operator
    /// </summary>
    public class TapeOperator
    {
        #region Types

        [StructLayout(LayoutKind.Sequential)]
        public struct MediaInfo
        {
            public long Capacity;
            public long Remaining;

            public uint BlockSize;
            public uint PartitionCount;

            public byte IsWriteProtected;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct MediaInfoSet
        {
            public uint BlockSize;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct DriveInfo
        {
            public byte ECC;
            public byte Compression;
            public byte DataPadding;
            public byte ReportSetMarks;

            public uint DefaultBlockSize;
            public uint MaximumBlockSize;
            public uint MinimumBlockSize;
            public uint PartitionCount;

            public uint FeaturesLow;
            public uint FeaturesHigh;
            public uint EOTWarningZoneSize;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct DriveInfoSet
        {
            public byte ECC;
            public byte Compression;
            public byte DataPadding;
            public byte ReportSetMarks;
            public uint EOTWarningZoneSize;
        }
        #endregion

        #region Private constants
        private const short FILE_ATTRIBUTE_NORMAL = 0x80;
        private const short INVALID_HANDLE_VALUE = -1;
        private const uint GENERIC_READ = 0x80000000;
        private const uint GENERIC_WRITE = 0x40000000;
        private const uint CREATE_NEW = 1;
        private const uint CREATE_ALWAYS = 2;
        private const uint OPEN_EXISTING = 3;
        private const uint FILE_ATTRIBUTE_ARCHIVE = 0x00000020;
        private const uint FILE_FLAG_BACKUP_SEMANTICS = 0x02000000;

        private const uint NO_ERROR = 0;

        //The below are documented at http://msdn.microsoft.com/en-us/library/windows/desktop/aa362532(v=vs.85).aspx
        private const int TAPE_LOAD = 0;
        private const int TAPE_UNLOAD = 1;
        //I added the below based on the above mentioned article
        private const int TAPE_TENSION = 2;
        private const int TAPE_LOCK = 3;
        private const int TAPE_UNLOCK = 4;
        private const int TAPE_FORMAT = 5;

        private const int TAPE_RELATIVE_BLOCKS = 5;

        private const int TAPE_LOGICAL_BLOCK = 2;
        private const int TAPE_LOGICAL_POSITION = 1;

        private const int FALSE = 0;
        private const int TRUE = 0;

        private const int MEDIA_PARAMS = 0;
        private const int DRIVE_PARAMS = 1;

        private const int SET_TAPE_MEDIA_INFORMATION = 0;
        private const int SET_TAPE_DRIVE_INFORMATION = 1;
        #endregion

        #region PInvoke
        // Use interop to call the CreateFile function.
        // For more information about CreateFile,
        // see the unmanaged MSDN reference library.
        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern SafeFileHandle CreateFile(
            string lpFileName,
            uint dwDesiredAccess,
            uint dwShareMode,
            IntPtr lpSecurityAttributes,
            uint dwCreationDisposition,
            uint dwFlagsAndAttributes,
            IntPtr hTemplateFile
            );

        [DllImport("kernel32", SetLastError = true)]
        private static extern int PrepareTape(
            SafeFileHandle handle,
            int prepareType,
            BOOL isImmediate
            );


        [DllImport("kernel32", SetLastError = true)]
        private static extern int SetTapePosition(
            SafeFileHandle handle,
            int positionType,
            int partition,
            int offsetLow,
            int offsetHigh,
            BOOL isImmediate
            );

        [DllImport("kernel32", SetLastError = true)]
        private static extern int GetTapePosition(
            SafeFileHandle handle,
            int positionType,
            out int partition,
            out int offsetLow,
            out int offsetHigh
            );

        [DllImport("kernel32", SetLastError = true)]
        private static extern int GetTapeParameters(
           SafeFileHandle handle,
           int operationType,
           ref int size,
           IntPtr mediaInfo
           );

        //if the operationType is 1, then mediaInfo is the DriveInfoSet struct; if operationType is 0 then mediaInfo is MediaInfoSet
        [DllImport("kernel32", SetLastError = true)]
        private static extern int SetTapeParameters(
           SafeFileHandle handle,
           int operationType,
           IntPtr mediaInfo
           );

        [DllImport("kernel32", SetLastError = true)]
        private static extern int GetLastError();
        #endregion

        #region Private variables
        private FileStream m_stream;
        private SafeFileHandle m_handleValue = null;

        private Nullable<DriveInfo> m_driveInfo = null;
        #endregion

        #region Public methods

        /// <summary>
        /// Loads tape with given name. 
        /// </summary>
        public void Load(string tapeName)
        {
            // Try to open the file.
            //m_handleValue = CreateFile(
            //    tapeName,
            //    GENERIC_READ | GENERIC_WRITE,
            //    0,
            //    IntPtr.Zero,
            //    OPEN_EXISTING,
            //    FILE_ATTRIBUTE_ARCHIVE | FILE_FLAG_BACKUP_SEMANTICS,
            //    IntPtr.Zero
            //    );
            m_handleValue = CreateFile(
                tapeName,
                GENERIC_READ | GENERIC_WRITE,
                0,
                IntPtr.Zero,
                OPEN_EXISTING,
                FILE_ATTRIBUTE_ARCHIVE | FILE_FLAG_BACKUP_SEMANTICS,
                IntPtr.Zero
                );

            if (m_handleValue.IsInvalid)
            {
                throw new TapeOperatorWin32Exception(
                    "CreateFile", Marshal.GetLastWin32Error());
            }

            // Load the tape
            int result = PrepareTape(
                m_handleValue,
                TAPE_LOAD,
                TRUE
                );

            if (result != NO_ERROR)
            {
                throw new TapeOperatorWin32Exception(
                     "PrepareTape", Marshal.GetLastWin32Error());
            }

            m_stream = new FileStream(
                m_handleValue,
                FileAccess.ReadWrite,
                65536,
                false
                );
        }

        /// <summary>
        /// Writes to the tape given stream starting from given postion
        /// </summary>
        /// <param name="startPos"></param>
        /// <param name="stream"></param>
        public void Write(long startPos, byte[] stream)
        {
            // Get number of blocks that will be nned to perform write
            uint numberOfBlocks = GetBlocksNumber(stream.Length);

            // Updates tape's current position
            SetTapePosition(startPos);

            byte[] arrayToWrite = new byte[numberOfBlocks * BlockSize];
            Array.Copy(stream, arrayToWrite, stream.Length);

            // Write data to the device
            m_stream.Write(stream, 0, stream.Length);
            m_stream.Flush();
        }

        /// <summary>
        /// Read one logical block from tape 
        /// starting on the given position
        /// </summary>
        /// <returns></returns>
        public byte[] Read(long startPosition)
        {
            byte[] buffer = new byte[BlockSize];

            SetTapePosition(startPosition);

            m_stream.Read(buffer, 0, buffer.Length);
            m_stream.Flush();

            return buffer;
        }

        /// <summary>
        /// Read given number of bytes starting 
        /// on the given position
        /// </summary>
        public byte[] Read(long startPosition, long bytes)
        {
            uint blocksNumber = GetBlocksNumber(bytes);
            int module = Convert.ToInt32(bytes % BlockSize);

            byte[] buffer = new byte[bytes];

            for (uint i = 0; i < blocksNumber; i++)
            {
                byte[] temp = Read(i + startPosition);

                if (i + 1 != blocksNumber)
                {
                    Array.Copy(temp, 0, buffer, BlockSize * i, BlockSize);
                }
                else
                {
                    Array.Copy(temp, 0, buffer, BlockSize * i, module);
                }

            }// for

            return buffer;
        }

        /// <summary>
        /// Checks if tape can be read from the
        /// given position
        /// </summary>
        public bool CanRead(long startPosition)
        {
            bool status = true;
            long pos = GetTapePosition();

            try
            {
                Read(startPosition);
            }
            catch
            {
                status = false;
            }
            finally
            {
                SetTapePosition(pos);
            }

            return status;
        }

        /// <summary>
        /// Checks if given number of bytes can be read
        /// </summary>
        public bool CanRead(long startPosition, long bytes)
        {
            bool status = true;
            long pos = GetTapePosition();

            try
            {
                Read(startPosition, bytes);
            }
            catch
            {
                status = false;
            }
            finally
            {
                SetTapePosition(pos);
            }

            return status;
        }

        /// <summary>
        /// Closes handler of the current tape
        /// </summary>
        public void Close()
        {
            if (m_handleValue != null &&
                !m_handleValue.IsInvalid &&
                !m_handleValue.IsClosed)
            {
                m_handleValue.Close();
            }
        }

        /// <summary>
        /// Sets new tape position ( current seek )
        /// </summary>
        /// <param name="logicalBlock"></param>
        public void SetTapePosition(long logicalBlock)
        {
            int errorCode = 0;

            // TODO: reapit it
            if ((errorCode = SetTapePosition(
               m_handleValue,
               TAPE_LOGICAL_BLOCK,
               0,
               (int)logicalBlock,
               0,
               TRUE)) != NO_ERROR)
            {
                throw new TapeOperatorWin32Exception(
                    "SetTapePosition", Marshal.GetLastWin32Error());
            }
        }

        /// <summary>
        /// Returns Current tape's postion ( seek )
        /// </summary>
        /// <returns></returns>
        public long GetTapePosition()
        {
            int partition;
            int offsetLow;
            int offsetHigh;

            if (GetTapePosition(
                m_handleValue,
                TAPE_LOGICAL_POSITION,
                out partition,
                out offsetLow,
                out offsetHigh) != NO_ERROR)
            {
                throw new TapeOperatorWin32Exception(
                    "GetTapePosition", Marshal.GetLastWin32Error());
            }

            long offset = (long)(offsetHigh * Math.Pow(2, 32) + offsetLow);

            return offset;
        }
        
        /// <summary>
        /// I added the below public methods by utilizing the calls in the PInvoke region
        /// </summary>
        /// <returns></returns>
        public MediaInfo GetTapeMediaParameters()
        {
            IntPtr ptr = IntPtr.Zero;
            try
            {
                MediaInfo mediaInfo = new MediaInfo();

                // Allocate unmanaged memory
                int size = Marshal.SizeOf(mediaInfo);
                ptr = Marshal.AllocHGlobal(size);

                Marshal.StructureToPtr(
                mediaInfo,
                ptr,
                false
                );


                int result = 0;

                if ((result = GetTapeParameters(
                m_handleValue,
                MEDIA_PARAMS,
                ref size,
                ptr)) != NO_ERROR)
                {
                    throw new TapeOperatorWin32Exception(
                    "GetTapeParameters",
                    Marshal.GetLastWin32Error());
                }

                // Get managed media Info
                mediaInfo = (MediaInfo)
                Marshal.PtrToStructure(ptr, typeof(MediaInfo));

                return mediaInfo;
            }
            finally
            {
                if (ptr != IntPtr.Zero) Marshal.FreeHGlobal(ptr);
            }
        }

        public void SetTapeMediaParameters(MediaInfoSet objMediaInfo)
        {
            IntPtr ptr = IntPtr.Zero;
            try
            {
                // Allocate unmanaged memory
                int size = Marshal.SizeOf(objMediaInfo);
                ptr = Marshal.AllocHGlobal(size);

                //create a pointer for the structure objDriveInfo
                Marshal.StructureToPtr(
                        objMediaInfo,
                        ptr,
                        false
                    );

                int result = 0;
                if ((result = SetTapeParameters(
                    m_handleValue,
                    SET_TAPE_MEDIA_INFORMATION,
                    ptr)) != NO_ERROR)
                {
                    throw new TapeOperatorWin32Exception(
                        "SetTapeMediaParameters", Marshal.GetLastWin32Error());
                }

            }
            finally
            {
                if (ptr != IntPtr.Zero)
                {
                    Marshal.FreeHGlobal(ptr);
                }
            }
        }

        public DriveInfo GetTapeDriveParameters()
        {
            IntPtr ptr = IntPtr.Zero;
            try
            {
                if (!m_driveInfo.HasValue)
                {
                    m_driveInfo = new DriveInfo();

                    // Allocate unmanaged memory
                    int size = Marshal.SizeOf(m_driveInfo);
                    ptr = Marshal.AllocHGlobal(size);

                    Marshal.StructureToPtr(
                        m_driveInfo,
                        ptr,
                        false
                    );


                    int result = 0;
                    if ((result = GetTapeParameters(
                        m_handleValue,
                        DRIVE_PARAMS,
                        ref size,
                        ptr)) != NO_ERROR)
                    {
                        throw new TapeOperatorWin32Exception(
                            "GetTapeParameters", Marshal.GetLastWin32Error());
                    }

                    // Get managed media Info
                    m_driveInfo = (DriveInfo)
                        Marshal.PtrToStructure(ptr, typeof(DriveInfo));
                }


                return (DriveInfo)m_driveInfo;
            }
            finally
            {
                if (ptr != IntPtr.Zero)
                {
                    Marshal.FreeHGlobal(ptr);
                }
            }
        }

        public void SetTapeDriveParameters(DriveInfoSet objDriveInfo)
        {
            IntPtr ptr = IntPtr.Zero;
            try
            {
                // Allocate unmanaged memory
                int size = Marshal.SizeOf(objDriveInfo);
                ptr = Marshal.AllocHGlobal(size);

                //create a pointer for the structure objDriveInfo
                Marshal.StructureToPtr(
                        objDriveInfo,
                        ptr,
                        false
                    );

                int result = 0;
                if ((result = SetTapeParameters(
                    m_handleValue,
                    SET_TAPE_DRIVE_INFORMATION,
                    ptr)) != NO_ERROR)
                {
                    throw new TapeOperatorWin32Exception(
                        "SetTapeDriveParameters", Marshal.GetLastWin32Error());
                }

            }
            finally
            {
                if (ptr != IntPtr.Zero)
                {
                    Marshal.FreeHGlobal(ptr);
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public void FormatTape()
        {
            if (m_handleValue.IsInvalid)
            {
                throw new TapeOperatorWin32Exception(
                    "CreateFile", Marshal.GetLastWin32Error());
            }

            // Format the tape
            int result = PrepareTape(
                m_handleValue,
                TAPE_FORMAT,
                TRUE //when this is false the function won't return until the operation is completed
                );

            if (result != NO_ERROR)
            {
                throw new TapeOperatorWin32Exception(
                     "FormatTape", Marshal.GetLastWin32Error());
            }
        }

        /// <summary>
        /// Moves to the end of the tape and rewinds to the beginning of the tape. This value is ignored if the tape device does not support tensioning.
        /// </summary>
        public void UnloadTape()
        {
            if (m_handleValue.IsInvalid)
            {
                throw new TapeOperatorWin32Exception(
                    "CreateFile", Marshal.GetLastWin32Error());
            }

            // Format the tape
            int result = PrepareTape(
                m_handleValue,
                TAPE_UNLOAD,
                FALSE //when this is false the function won't return until the operation is completed
                );

            if (result != NO_ERROR)
            {
                throw new TapeOperatorWin32Exception(
                     "Unload Tape", Marshal.GetLastWin32Error());
            }
        }

        public void TensionTape()
        {
            if (m_handleValue.IsInvalid)
            {
                throw new TapeOperatorWin32Exception(
                    "CreateFile", Marshal.GetLastWin32Error());
            }

            // Format the tape
            int result = PrepareTape(
                m_handleValue,
                TAPE_TENSION,
                TRUE //when this is false the function won't return until the operation is completed
                );

            if (result != NO_ERROR)
            {
                throw new TapeOperatorWin32Exception(
                     "Tension Tape", Marshal.GetLastWin32Error());
            }
        }

        #endregion

        #region Public properties

        /// <summary>
        /// Retruns opened file handle
        /// </summary>
        public SafeFileHandle Handle
        {
            get
            {
                // If the handle is valid,
                // return it.
                if (!m_handleValue.IsInvalid)
                {
                    return m_handleValue;
                }
                else
                {
                    return null;
                }
            }// GET
        }

        /// <summary>
        /// Returns default block size for current
        /// device
        /// </summary>
        public uint BlockSize
        {
            get
            {
                IntPtr ptr = IntPtr.Zero;
                try
                {
                    if (!m_driveInfo.HasValue)
                    {
                        m_driveInfo = new DriveInfo();

                        // Allocate unmanaged memory
                        int size = Marshal.SizeOf(m_driveInfo);
                        ptr = Marshal.AllocHGlobal(size);

                        Marshal.StructureToPtr(
                            m_driveInfo,
                            ptr,
                            false
                        );


                        int result = 0;
                        if ((result = GetTapeParameters(
                            m_handleValue,
                            DRIVE_PARAMS,
                            ref size,
                            ptr)) != NO_ERROR)
                        {
                            throw new TapeOperatorWin32Exception(
                                "GetTapeParameters", Marshal.GetLastWin32Error());
                        }

                        // Get managed media Info
                        m_driveInfo = (DriveInfo)
                            Marshal.PtrToStructure(ptr, typeof(DriveInfo));
                    }


                    return m_driveInfo.Value.DefaultBlockSize;
                    //return 512;
                }
                finally
                {
                    if (ptr != IntPtr.Zero)
                    {
                        Marshal.FreeHGlobal(ptr);
                    }
                }
            }
        }
        #endregion

        #region Private methods

        /// <summary>
        /// Returns minum number of blocks that can contain
        /// given number of bytes
        /// </summary>
        private uint GetBlocksNumber(long bytes)
        {
            uint numberOfBlocks = (uint)bytes / BlockSize;
            uint bytesInLastBlock = (uint)bytes % BlockSize;

            // Calculate number of blocks
            if (bytesInLastBlock > 0) numberOfBlocks++;

            return numberOfBlocks;
        }
        #endregion

    }

    /// <summary>
    /// Exception that will be thrown by tape
    /// operator when one of WIN32 APIs terminates 
    /// with error code 
    /// </summary>
    public class TapeOperatorWin32Exception : ApplicationException
    {
        public TapeOperatorWin32Exception(string methodName, int win32ErroCode) :
            base(string.Format(
               "WIN32 API method failed : {0} failed with error code {1}",
               methodName,
               win32ErroCode
           )) { }
    }
}
