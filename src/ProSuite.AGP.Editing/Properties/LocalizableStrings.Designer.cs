﻿//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.30319.42000
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace ProSuite.AGP.Editing.Properties {
    using System;
    
    
    /// <summary>
    ///   A strongly-typed resource class, for looking up localized strings, etc.
    /// </summary>
    // This class was auto-generated by the StronglyTypedResourceBuilder
    // class via a tool like ResGen or Visual Studio.
    // To add or remove a member, edit your .ResX file then rerun ResGen
    // with the /str option, or rebuild your VS project.
    [global::System.CodeDom.Compiler.GeneratedCodeAttribute("System.Resources.Tools.StronglyTypedResourceBuilder", "16.0.0.0")]
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
    [global::System.Runtime.CompilerServices.CompilerGeneratedAttribute()]
    internal class LocalizableStrings {
        
        private static global::System.Resources.ResourceManager resourceMan;
        
        private static global::System.Globalization.CultureInfo resourceCulture;
        
        [global::System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        internal LocalizableStrings() {
        }
        
        /// <summary>
        ///   Returns the cached ResourceManager instance used by this class.
        /// </summary>
        [global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Advanced)]
        internal static global::System.Resources.ResourceManager ResourceManager {
            get {
                if (object.ReferenceEquals(resourceMan, null)) {
                    global::System.Resources.ResourceManager temp = new global::System.Resources.ResourceManager("ProSuite.AGP.Editing.Properties.LocalizableStrings", typeof(LocalizableStrings).Assembly);
                    resourceMan = temp;
                }
                return resourceMan;
            }
        }
        
        /// <summary>
        ///   Overrides the current thread's CurrentUICulture property for all
        ///   resource lookups using this strongly typed resource class.
        /// </summary>
        [global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Advanced)]
        internal static global::System.Globalization.CultureInfo Culture {
            get {
                return resourceCulture;
            }
            set {
                resourceCulture = value;
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to {0}&lt;br&gt;Press [ESC] to select one or more different features..
        /// </summary>
        internal static string RemoveOverlapsTool_AfterSelection {
            get {
                return ResourceManager.GetString("RemoveOverlapsTool_AfterSelection", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Remove Overlaps.
        /// </summary>
        internal static string RemoveOverlapsTool_Caption {
            get {
                return ResourceManager.GetString("RemoveOverlapsTool_Caption", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Select one or more line or polygon features from which a part should be removed. Press [O] for additional options.&lt;br&gt;- Press and hold SHIFT to add or remove features from the existing selection.&lt;br&gt;- Press and hold P to draw a polygon that completely contains the features to be selected. Finish the polygon with double-click..
        /// </summary>
        internal static string RemoveOverlapsTool_LogPromptForSelection {
            get {
                return ResourceManager.GetString("RemoveOverlapsTool_LogPromptForSelection", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Remove a part of a feature that overlaps with other polygon features..
        /// </summary>
        internal static string RemoveOverlapsTool_Message {
            get {
                return ResourceManager.GetString("RemoveOverlapsTool_Message", resourceCulture);
            }
        }
    }
}