# -*- coding: utf-8 -*-

import arcpy


class Toolbox(object):
    def __init__(self):
        """Define the toolbox (the name of the toolbox is the name of the
        .pyt file)."""
        self.label = "Carto Extras"
        self.alias = "Carto Extras"

        # List of tool classes associated with this toolbox
        self.tools = [ArcadeExpressionToField]


class ArcadeExpressionToField(object):
    def __init__(self):
        """Define the tool (tool name is the name of the class)."""
        self.label = "Evaluate UVR Expression To Field"
        self.description = "For each feature layer in the selected map that uses " \
                           "the Unique Value Renderer with an Arcade expression, " \
                           "evaluate that expression and write the resulting value " \
                           "to a field. Optionally, update the renderer to use this " \
                           "field instead of the Arcade expression."
        self.canRunInBackground = False

    def getParameterInfo(self):
        """Parameter definitions"""
        paramMapName = arcpy.Parameter(
            name='map_name',
            displayName='Map Name',
            datatype='Map',
            direction='Input',
            parameterType='Required')

        paramFieldName = arcpy.Parameter(
            name='field_name',
            displayName='Name Of Field To Create',
            datatype='String',
            direction='Input',
            parameterType='Required')
        paramFieldName.value = 'SYMTYPE'  # default value

        paramUseField = arcpy.Parameter(
            name='use_field',
            displayName='Update Renderer To Use New Field ',
            datatype='Boolean',
            direction='Input',
            parameterType='Required')
        paramUseField.value = False  # default value

        return [paramMapName, paramFieldName, paramUseField]

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
        mapName = parameters[0].valueAsText
        fieldName = parameters[1].valueAsText
        useField = parameters[2].value
        arcade_to_field(mapName, fieldName, useField)
        return

    def postExecute(self, parameters):
        """This method takes place after outputs are processed and
        added to the display."""
        return


def hasField(fClass, fieldName):
    fields = arcpy.ListFields(fClass)
    for f in fields:
        if f.name == fieldName:
            return True
    return False


def arcade_to_field(mapName, fieldName, useField):
    aprx: arcpy.mp.ArcGISProject = arcpy.mp.ArcGISProject("CURRENT")
    map = aprx.listMaps(mapName)[0]
    layers = map.listLayers()

    for layer in layers:
        if not layer.isFeatureLayer: continue
        if not layer.supports('SYMBOLOGY'): continue
        sym = layer.symbology
        if not hasattr(sym, 'renderer'): continue
        if not sym.renderer.type == 'UniqueValueRenderer': continue

        try:
            cim = layer.getDefinition('V3')
            if (cim.renderer and cim.renderer.valueExpressionInfo and
                cim.renderer.valueExpressionInfo.expression):
                # if field exists, delete it, then add the new field
                if hasField(layer, fieldName):
                    arcpy.AddMessage(f"Deleting field {fieldName} from {layer.name}")
                    arcpy.DeleteField_management(layer, [fieldName])

                arcpy.AddMessage(f"Adding field {fieldName} to {layer.name}")
                arcpy.AddField_management(layer, fieldName, 'TEXT', field_length=255)

                expr = cim.renderer.valueExpressionInfo.expression
                arcpy.AddMessage(f"Calculating field {fieldName} on layer {layer.name} as\n{expr}")
                arcpy.CalculateField_management(layer, fieldName, expr, 'ARCADE')

                if useField:
                    cim.renderer.fields = [fieldName]
                    cim.renderer.valueExpressionInfo = None
                    arcpy.AddMessage(f"Updating UV renderer to use new field {fieldName}")
                    layer.setDefinition(cim)
        except Exception as e:
            arcpy.AddWarning(f'Skipping layer {layer.name}: Could not calculate field:\n{e.args[0]}')
