## Latex Sandcastle

Forked from [EWSoftware/latex-sandcastle](https://github.com/EWSoftware/latex-sandcastle). This Fork replaces the mimetex dependency with [wpf-math](https://github.com/ForNeVeR/wpf-math). wpf-math is an active C# repository with the ability to output equations in SVG format. The aim to provide more flexibility and make the build process simpler.  

The code in this project has been implemented with personal use in mind, so it could benefit from some refactoring.    

Requirements:
Sandcastle Help File Builder v2015.7.25.0 or later

Instructions

1. Build the LatexBuildComponent project.
2. From the *source\out\\* folder, copy the two DLLs into the Sandcastle Help File Builder (SHFB) Components and Plug-Ins directory.  This directory differs depending on your OS.

   Windows Vista, 7, & 8: *%ProgramData%\EWSoftware\Sandcastle Help File Builder\Components and Plug-Ins*

   Windows XP: *%ALLUSERSPROFILE%\Application Data\EWSoftware\Sandcastle Help File Builder\Components and Plug-Ins*

3. In your SHFB project file, add the "LaTeX Build Component" to the project in the *Components* project property category.
4. Add `<latex>` tags to your XML comments.  The `<latex>` tag must be placed inside regular XML comment tags.

   Example: `///<summary><latex>f(x)=x^2</latex></summary>`

   For complex LaTeX code, it should be placed in a CDATA tag, i.e. `<latex><!CDATA[f(x)=x^2]]></latex>`

   The LaTeX code can be inside the latex tag, or in an "expr" attribute of the latex tag.

   Example: `///<summary><latex expr="f(x)=x^2"/></summary>`

   If code is in both the expr attribute and the tag body, the code in the attribute is used and the body code is discarded.

5. Run SHFB as usual.
