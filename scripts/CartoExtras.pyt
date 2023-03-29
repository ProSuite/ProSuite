# -*- coding: utf-8 -*-

import arcpy


class Toolbox(object):
    def __init__(self):
        """Define the toolbox (the name of the toolbox is the name of the
        .pyt file)."""
        self.label = "Toolbox"
        self.alias = "toolbox"

        # List of tool classes associated with this toolbox
        self.tools = [ArcadeExpressionToField]


class ArcadeExpressionToField(object):
    def __init__(self):
        """Define the tool (tool name is the name of the class)."""
        self.label = "Evaluate UV Expression To Field"
        self.description = ""
        self.canRunInBackground = False

    def getParameterInfo(self):
        """Define parameter definitions"""
        paramMapName = arcpy.Parameter(
            name='map_name',
            displayName='Map Name',
            datatype='Map',
            direction='Input',
            parameterType='Required'
        )
        paramFieldName = arcpy.Parameter(
            name='field_name',
            displayName='Name Of Field To Create',
            datatype='String',
            direction='Input',
            parameterType='Required'
        )
        paramFieldName.value = 'SYMTYPE'

        paramUseField = arcpy.Parameter(
            name='use_field',
            displayName='Update Renderer To Use New Field ',
            datatype='Boolean',
            direction='Input',
            parameterType='Required'
        )
        paramUseField.value = True

        params = [paramMapName, paramFieldName, paramUseField]
        return params

    def isLicensed(self):
        """Set whether tool is licensed to execute."""
        return True

    def updateParameters(self, parameters):
        """Modify the values and properties of parameters before internal
        validation is performed.  This method is called whenever a parameter
        has been changed."""
        return

    def updateMessages(self, parameters):
        """Modify the messages created by internal validation for each tool
        parameter.  This method is called after internal validation."""
        return

    def execute(self, parameters, messages):
        """The source code of the tool."""
        arcade_to_field(
            parameters[0].valueAsText, parameters[1].valueAsText, parameters[2].value)

        return

    def postExecute(self, parameters):
        """This method takes place after outputs are processed and
        added to the display."""
        return


def hasField(field, fClass):
    fields = arcpy.ListFields(fClass)
    for f in fields:
        if f.name == field:
            return True
    return False


def CalculateField(fClass, valueExpressionInfo, fieldName, layer):
    arcpy.AddMessage('Calculating field ' + fieldName + ' for ' +
                     layer.name + '\n' + valueExpressionInfo.expression)
    arcpy.CalculateField_management(
        fClass, fieldName, valueExpressionInfo.expression, 'ARCADE')


def arcade_to_field(mapName, fieldName, useField):
    aprx: arcpy.mp.ArcGISProject = arcpy.mp.ArcGISProject("CURRENT")
    map = aprx.listMaps(mapName)[0]
    layers = map.listLayers()

    for layer in layers:
        if layer.isFeatureLayer:
            if layer.supports('SYMBOLOGY'):
                sym = layer.symbology
                if hasattr(sym, 'renderer'):
                    if sym.renderer.type == 'UniqueValueRenderer':
                        try:
                            cim = layer.getDefinition('V3')
                            if (cim.renderer and cim.renderer.valueExpressionInfo and
                                cim.renderer.valueExpressionInfo.expression):
                                # if field exists, delete it, then add the new field
                                if hasField(fieldName, layer):
                                    arcpy.DeleteField_management(layer, [fieldName])
                                    arcpy.AddMessage(
                                        'Deleted field ' + fieldName + ' from '+layer.name)

                                arcpy.AddField_management(
                                    layer, fieldName, 'TEXT', field_length=255)
                                arcpy.AddMessage(
                                    'Added field ' + fieldName + ' to '+layer.name)
                                CalculateField(
                                    layer, cim.renderer.valueExpressionInfo, fieldName, layer)
                                arcpy.AddMessage(
                                    'Calculated field ' + fieldName + ' for '+layer.name)
                                if useField:
                                    cim.renderer.fields = [fieldName]
                                    cim.renderer.valueExpressionInfo = None
                                    layer.setDefinition(cim)
                                    arcpy.AddMessage(
                                        f'Updated renderer to use new field "{fieldName}"')
                        except Exception as e:
                            arcpy.AddWarning(
                                'WARNING: Could not calculate field for ' + layer.name + '\n' + e.args[0] + '\n')
