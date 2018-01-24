# XPathVisualizer v1.3
The XPathVisualizer application is easy to use, and mostly self-explanatory.

## A Summary of the Obvious Parts:

- Load an XML into the application from a file or URL with File...Open or File...Get
- You can also paste XML into the XML doc window, and you can edit the document directly.
- The XML is automatically syntax-highlighted. This happens in the background. For large docs it takes a few seconds but you can still edit the XML while highlighting is happening.
- The XML namespaces are automatically detected and listed in the form. If there's no prefix assigned to a particular namespace, then one is assigned. All the prefixes are available in xpath expressions that you can apply to the document.
- If you'd like to specify an additional XML Namespace and prefix, you can do so in the provided boxes. Press the "+" button (![xpv-plus.png](./docs/xpv-plus.png)) to add it to the list.
- For any XML Namespace that you don't want to display, click the "X" button (![xpv-x.png](./docs/xpv-x.png)) to remove it from the list.
- Specify the XPath expression in the provided box. It is evaluated automatically.
- Use Edit....Reformet to reformat (re-indent) the XML. You can also do this via the keyboard with control-F.
- Strip namespaces with Edit....Strip namespaces
- Toggle line numbers via Edit...Line Numbers, or with control-L .
- right click on the box to select a query from a list of the 10 most-recently used xpath query expressions.
- use the Help menu to discover other features.


## Some other interesting pieces.

- XPathVisualizer provides you a navigator panel to click through all the matches. This is especially helpful with a large document.

  ![xpv-matchNavigator.png](./docs/XPV-matchNavigator.png)

- XPathVisualizer tells you if there is an error in the xpath expression:

  ![xpv-UnknownXPathFunction.png](./docs/xpv-UnknownXPathFunction.png)

- If the expression is valid, it tells you how many nodes match, and it highlights those nodes in the document.

  ![xpv-NodesMatched.png](./docs/XPV-NodesMatched.png)
  