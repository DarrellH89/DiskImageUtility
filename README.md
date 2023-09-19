# DiskImageUtility

Disk Image Utility for Flash Floppy Gotek Heathkit H-89 and Z-100 Disk Image Utility is designed to support using Flash Floppy or HxC flashed Gotek drives with Heathkit 1980 era computers. It supports the native disk formats using IMG files and allows you to extract and add files from your PC to the disk image file. It currently supports CP/M, HDOS, and MS-DOS FAT 12 formats used by the H-8, H-89, and Z-100.

If you want to copy files between images you must first extract the file and then insert the file in another image. Complete documentation is in the file Disk Utility.docx.

Disk Image Utility can also create blank disk images in several CP/M, HDOS, and MS-DOS formats which you can use in a Flash Floppy or HxC flashed Gotek. Disk Image Utility also supports adding files to these images. File deletion is currently only supoorted in HDOS.

## File Conversion Notes

* Dunfield IMD files image skew matches physical disk. For an 800k disk, the first sector is 3.
  ** H37 has skew of 3, sector 1 is first but in same order as Dunfield IMD
   _ To convert H37 to IMG
     _ Convert to IMD
     _ Then convert to IMG
   _ MS-DOS IMD disk has skew = 1
   _ IMG files are sequential order, skew = 1
   _ IMG conversion to IMD keeps skew = 1
   _ IMD conversion to IMG changes skew to 1

# Change Log
- 1.1e
  _ Added code for Livingston Logic Labs for H8D
  _ Added IMG to IMD conversion
  _ MessageBox Title Update. Changed Disk Utility to Disk Image Utility
-	1.1f
__	Corrected program exception error when adding files
- 1.1g
__	Changed IMD conversion to report a fatal error if the number of sectors per track changes between tracks.
__	Added ability to handle IMD cylinder map data (ignore it).
__	Delete converted file if an error occurs. Improved file conversion abort code.
_	1.2
__	Added HDoS Support
__	Supports both HDOS 3 and HDOS 2 disks
_	1.2a
__	Added support for MS-DOS 1.2MB
__	Added HDoS file delete
__	Added support for .H37 files with disk information at the end of the file name.
_	1.2b 
__	Fixed bug in CP/M extract caused by added support for .H37 files.
