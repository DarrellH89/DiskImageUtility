# DiskImageUtility

Disk Image Utility for Flash Floppy Gotek Heathkit H-89 and Z-100 Disk Image Utility is designed to support using Flash Floppy or HxC flashed Gotek drives with Heathkit 1980 era computers. It supports the native disk formats using IMG files and allows you to extract and add files from your PC to the disk image file. It currently supports CP/M, HDOS, and MS-DOS FAT 12 formats used by the H-8, H-89, and Z-100.

If you want to copy files between images you must first extract the file and then insert the file in another image. Complete documentation is in the file Disk Utility.docx.

Disk Image Utility can also create blank disk images in several CP/M, HDOS, and MS-DOS formats which you can use in a Flash Floppy or HxC flashed Gotek. Disk Image Utility also supports adding files to these images. File deletion is currently only supoorted in HDOS.

# File Conversion Notes

_	Dunfield IMD files image skew matches physical disk. For an 800k disk, the first sector is 3.
_	H37 has skew of 3, sector 1 is first but in same order as Dunfield IMD
__	To convert H37 to IMG
___	Convert to IMD
___	Then convert to IMG
_	MS-DOS IMD disk has skew = 1
_	IMG files are sequential order, skew = 1
_	IMG conversion to IMD keeps skew = 1
_	IMD conversion to IMG changes skew to 1

# Change Log
-	1.1e
__	Added c__de f__r Livingst__n L__gic Labs f__r H8D
__	Added IMG t__ IMD c__nversi__n
__	MessageB__x Title Update. Changed Disk Utility t__ Disk Image Utility
-	1.1f
__	C__rrected pr__gram excepti__n err__r when adding files
- 1.1g
__	Changed IMD c__nversi__n t__ rep__rt a fatal err__r if the number __f sect__rs per track changes between tracks.
__	Added ability t__ handle IMD cylinder map data (ign__re it).
__	Delete c__nverted file if an err__r __ccurs. Impr__ved file c__nversi__n ab__rt c__de.
_	1.2
__	Added HD__S Supp__rt
__	Supp__rts b__th HD__S 3 and HD__S 2 disks
_	1.2a
__	Added supp__rt f__r MS-D__S 1.2MB
__	Added HD__S file delete
__	Added supp__rt f__r .H37 files with disk inf__rmati__n at the end __f the file name.
_	1.2b 
__	Fixed bug in CP/M extract caused by added supp__rt f__r .H37 files.
