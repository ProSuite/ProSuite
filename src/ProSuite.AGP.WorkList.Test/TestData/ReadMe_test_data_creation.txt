How to create FGDB and feature class for ProSuite.AGP.WorkList.Test (daro, 23/07/20)
----------------------------------------------------------------------

# create FGDB, ensure correct path on your machine
arcpy.CreateFileGDB_management(r'C:\git\ProSuite\src\ProSuite.AGP.WorkList.Test\TestData', 'issues.gdb')

# import XML workspace document IssuePolygons.xml