TapeDrive
=========
This is a console application takes a list of files from an XML file named FilesToBackup.xml and writes them to a tape drive.
The output is a file called BackedUpFiles.xml which contains the necessary information to restore the files that were backed up
  to the tape drive.  A log file is also generated called log.txt which will display any exceptions that have occurred as well as 
  the start and end time of the write process.
TapeBackup.exe has several configurable switches that can be found with the switch /? (ie TapeBackup.exe /?)
