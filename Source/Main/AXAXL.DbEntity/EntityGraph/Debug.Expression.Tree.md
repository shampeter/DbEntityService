# Resources for Debugging Expression Trees

According to this [document](https://docs.microsoft.com/en-us/dotnet/csharp/programming-guide/concepts/expression-trees/debugging-expression-trees-in-visual-studio) from Microsoft,
there are 2 useful resources.

1. [Readable Expression](https://github.com/agileobjects/ReadableExpressions)

    - Can be installed as Visual Studio extension; and
    - Can be installed by Nuget package into the application to help printing expression tree.

1. [Express Tree Visualizer](https://github.com/zspitz/ExpressionToString#visual-studio-debugger-visualizer-for-expression-trees)

    - Can be used in code to print expression tree for debugging.
    - Can be installed as debug visualizer to help debugging effort within Visual Studio.

The latter one is chosen to be used due to it's simple API for printing expression tree.

- Assemblies are downloaded from this [location](https://github.com/zspitz/ExpressionToString/releases) from github.  The assemblies are
  copied to this location

    > C:\Program Files (x86)\Microsoft Visual Studio\2019\Community\Common7\Packages\Debugger\Visualizers

    Adjust the location according to the version of Visual Studio on your local machine.

- Nuget package is installed to aid printing epxression tree by

    ```pm
    PM> Install-Package ExpressionTreeToString
    ```
  