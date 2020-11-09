'''
arcpy.DeleteDomain_management(r'C:\git\ProSuite\src\ProSuite.AGP.WorkList.Test\TestData\issues_1.gdb', "CORRECTION_STATUS_CD")

arcpy.AddField_management(r'C:\git\ProSuite\src\ProSuite.AGP.WorkList.Test\TestData\issues_1.gdb\IssueRows', 'STATUS', "LONG", 10, None, None, 'Status', "NULLABLE", "NON_REQUIRED", "CORRECTION_STATUS_CD")
arcpy.DeleteField_management(r'C:\git\ProSuite\src\ProSuite.AGP.WorkList.Test\TestData\issues_1.gdb\IssueLines', 'STATUS')
'''
import os

connection = r'C:\git\ProSuite\src\ProSuite.AGP.WorkList.Test\TestData\issues.gdb'
arcpy.env.workspace = connection

cd = "CORRECTION_STATUS_CD"

arcpy.CreateDomain_management(connection, cd, "Correction status for work list", "LONG", "CODED")

dom_dict = {"100":"Not Corrected",
            "200":"Corrected"}
            
for code in sorted(dom_dict):
  arcpy.AddCodedValueToDomain_management(connection, cd, code, dom_dict[code])


arcpy.AddField_management(os.path.join(arcpy.env.workspace, 'IssueLines'), 'STATUS', "LONG", None, None, None, 'Status', "NULLABLE", "NON_REQUIRED", "CORRECTION_STATUS_CD")
arcpy.AssignDefaultToField_management(os.path.join(arcpy.env.workspace, 'IssueLines'), 'STATUS', 100)


arcpy.AddField_management(os.path.join(arcpy.env.workspace, 'IssueMultiPatches'), 'STATUS', "LONG", None, None, None, 'Status', "NULLABLE", "NON_REQUIRED", "CORRECTION_STATUS_CD")
arcpy.AssignDefaultToField_management(os.path.join(arcpy.env.workspace, 'IssueMultiPatches'), 'STATUS', 100)

arcpy.AddField_management(os.path.join(arcpy.env.workspace, 'IssuePoints'), 'STATUS', "LONG", None, None, None, 'Status', "NULLABLE", "NON_REQUIRED", "CORRECTION_STATUS_CD")
arcpy.AssignDefaultToField_management(os.path.join(arcpy.env.workspace, 'IssuePoints'), 'STATUS', 100)

arcpy.AddField_management(os.path.join(arcpy.env.workspace, 'IssuePolygons'), 'STATUS', "LONG", None, None, None, 'Status', "NULLABLE", "NON_REQUIRED", "CORRECTION_STATUS_CD")
arcpy.AssignDefaultToField_management(os.path.join(arcpy.env.workspace, 'IssuePolygons'), 'STATUS', 100)

arcpy.AddField_management(os.path.join(arcpy.env.workspace, 'IssueRows'), 'STATUS', "LONG", None, None, None, 'Status', "NULLABLE", "NON_REQUIRED", "CORRECTION_STATUS_CD")
arcpy.AssignDefaultToField_management(os.path.join(arcpy.env.workspace, 'IssueRows'), 'STATUS', 100)