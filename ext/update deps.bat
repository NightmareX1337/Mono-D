@echo off

set xsbin=C:\Program Files (x86)\Xamarin Studio\bin
set xsaddin=C:\Program Files (x86)\Xamarin Studio\Addins

echo \bin:
copy "%xsbin%\Mono.Addins.dll" .
copy "%xsbin%\Mono.Addins.xml" .
copy "%xsbin%\Mono.Addins.Setup.dll" .
copy "%xsbin%\Mono.Addins.Setup.xml" .
copy "%xsbin%\Mono.Debugging.dll" .
copy "%xsbin%\Mono.Debugging.xml" .
copy "%xsbin%\Mono.TextEditor.dll" .
copy "%xsbin%\Mono.TextEditor.xml" .
copy "%xsbin%\MonoDevelop.Core.dll" .
copy "%xsbin%\MonoDevelop.Core.xml" .
copy "%xsbin%\MonoDevelop.Ide.dll" .
copy "%xsbin%\MonoDevelop.Ide.xml" .
copy "%xsbin%\Newtonsoft.Json.dll" .
copy "%xsbin%\Newtonsoft.Json.xml" .
copy "%xsbin%\ICSharpCode.NRefactory.dll" .
copy "%xsbin%\ICSharpCode.NRefactory.xml" .
copy "%xsbin%\ICSharpCode.NRefactory.CSharp.dll" .
copy "%xsbin%\ICSharpCode.NRefactory.CSharp.xml" .
copy "%xsbin%\ICSharpCode.SharpZipLib.dll" .
copy "%xsbin%\ICSharpCode.SharpZipLib.xml" .
copy "%xsbin%\Xwt.dll" .
copy "%xsbin%\Xwt.xml" .

echo \Addins:
copy "%xsaddin%\MonoDevelop.Debugger\MonoDevelop.Debugger.dll" .
copy "%xsaddin%\MonoDevelop.Debugger\MonoDevelop.Debugger.xml" .
copy "%xsaddin%\MonoDevelop.DesignerSupport\MonoDevelop.DesignerSupport.dll" .
copy "%xsaddin%\MonoDevelop.DesignerSupport\MonoDevelop.DesignerSupport.xml" .
copy "%xsaddin%\MonoDevelop.GtkCore\MonoDevelop.GtkCore.dll" .
copy "%xsaddin%\MonoDevelop.GtkCore\MonoDevelop.GtkCore.xml" .
copy "%xsaddin%\MonoDevelop.GtkCore\libsteticui.dll" .
copy "%xsaddin%\MonoDevelop.GtkCore\libsteticui.xml" .
copy "%xsaddin%\MonoDevelop.GtkCore\libstetic.dll" .
copy "%xsaddin%\MonoDevelop.GtkCore\libstetic.xml" .
copy "%xsaddin%\MonoDevelop.Refactoring\MonoDevelop.Refactoring.dll" .
copy "%xsaddin%\MonoDevelop.Refactoring\MonoDevelop.Refactoring.xml" .
copy "%xsaddin%\DisplayBindings\SourceEditor\MonoDevelop.SourceEditor2.dll" .
copy "%xsaddin%\DisplayBindings\SourceEditor\MonoDevelop.SourceEditor2.xml" .