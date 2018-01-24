# XPath Visualizer
XPathVisualizer is a simple, free Winforms tool to help you visualize the results of XPath queries on XML documents. 

![XpathVisualizer-v1.3.png](./docs/XpathVisualizer-v1.3.png)

## What Good is it?
This is a simple WinForms tool, that lets you visualize the results of an XPath query on an XML file. 

### Features:
- load the XML document from a filesystem file, from an HTTP URL, type it in, or paste (Ctrl-V).
- automatically detect and display the XML namespaces in use, in the document, including those with and without explicit prefixes
- XPath query validation - red squigglies on an XPath query that makes no sense.
- Remembers 10 most-recently used XPath queries.
- automatically inject the default xml namespace into query expressions
- visualize the XPath query resultsion the XML document
- Easy scroll through the matched nodes of an XPath query
- strip XML namespaces from any document
- Remove matched nodes in the document
- Re-indent or reformat XML
- After modifying the XML in any way, you can save the modified XML document to a new file

It's simple, basic, and effective.

I looked around for tools for this purpose, and found no tool that was quite right. There were simple, free tools, but they didn't do enough. They didn't do XML syntax highlighting, or highlighting of the nodes selected by the XPath query. They didn't allow the use of XML namespace prefixes in the query. I also found more complex tools, some of them free, some of them not. These did lots of other things. But they were beyond my needs. I didn't want all those features. 

I created this option as a simple and effective alternative. It does more than the minimum, but it isn't cluttered with features.

## Pre-Requisites
This tool requires .NET v3.5 in order to run.

## What does it lack?
There is no xpath "designer". You still have to type in the xpath syntax, and you have to know what you want to type. There's no "intellisense" for xpath functions, for example. If you have an invalid xpath expression, this tool tells you that it is invalid, and also where and why it is invalid, but not what the valid thing ought to be. 

If you are an xpath neophyte, you're still going to need to refer to an xpath reference to get the expressions right. This tool will help you visualize the results, but you have to write the queries, the xpath expressions, manually. (There's an open work item to provide an XPath intellisense or auto-complete capability. Vote it up if you would like to see it included into the tool.) 

[Check out the documentation.](./docs/Usage.md)

## What is interesting in the source code?
The source uses the XPathDocument class to perform an xpath query. That part is pretty vanilla.

There are a few interesting bits:

1. The highlighting of matches in the XML document was a bit tricky. I could not get the http://msdn.microsoft.com/en-us/library/System.Windows.Forms.TextBoxBase.GetFirstCharIndexFromLine(VS.85).aspx to work properly. I used the method to determine where to start and stop highlighting for a particular xpath match. In particular, the method did not work when lines wrapped within the richtextbox - like when the actual line of text was 80 chars, but the richtextbox was only 60 chars wide. So I wrote my own method, that ignores the line wrap and works only with newlines in the text itself.

2. syntax-colorizing of the XML in the RichTextBox was another challenge. It's not difficult to use an XmlReader and colorize element names, brackets, and attributes. (Here again I needed to use the method described above (GetCharIndexFromLine)). There are two challenges: large XML documents, and allowing dynamic edits or changes while the colorizing proceeds. For challenge #1, for an XML file of 100k, it can take a loooong time, maybe 10 seconds, to do all the colorizing. A simple wait cursor will not do - this is just too long to ask a user to wait. So the colorizing must happen asynchronously. For challenge #2, the syntax colorizing needs to work well with UI input. In case the user types something into the textbox, including "overtyping", the colorizing needs to behave correctly considering those changes. The approach I took was to have a separate BackgroundWorker that constantly does colorizing. It starts when a change is detected in the RichTextBox text. It delays 600ms, and then checks for an intervening change. If no intervening change has occurred, then the user is not actively typing. The textbox is "stable" and the thread begins colorizing the XML text. If an intervening change is detected, then the thread just loops around, and starts waiting again. The colorize loop is just an XmlReader loop that goes through all elements in the document. On regular intervals, the thread checks to see if more changes have occurred in the text. If so, the thread stops the current formatting effort, and starts waiting again. It all sounds kind of complicated but it isn't complicated in code, and it works nicely. The UI is responsive and changes in the XML are more or less immediately colorized.

3. Three custom WinForms controls:

    1. a button that repeats, as long as you hold it, much like an arrow key on a scrollbar. This is a neat trick. Normal buttons in .NET don't repeat. This is what is used to scroll through the xpath matches.

    2. An extended RichTextBox that provides line numbers
    3. An extended TabControl that provides VisualStudio look & feel with curved tabs, and IE-like close buttons on each tab.

4. a collapsible panel, which holds the XML namespaces.

5. The final interesting bit is the XpathParser that I use to check the syntax of the xpath expression. The parser is taken from http://xpathparser.codeplex.com, and slightly modified. It provides the ability to highlight the part that does not pass the parse test. In the future it could provide the possibility to do "intellisense" on the xpath. I started work on that but didn't finish it yet.

All this is viewable in the source code.