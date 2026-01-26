## 推荐方法：安装NuGetForUnity包管理器（github）

1. 安装NuGetForUnity包管理器（github）：
   - 在Unity中打开 `Window > Package Manager`
   - 点击左上方 `+` 按钮并选择`Add package from git URL...`
   - 输入网址 `https://github.com/GlitchEnzo/NuGetForUnity.git?path=/src/NuGetForUnity` 并点击添加按钮
   - 重启unity
2. 在Unity中打开 `Window > NuGet > Manage NuGet Packages`
3. 搜索并安装：
   - Microsoft.CodeAnalysis.CSharp (5.0.0)
   - Microsoft.CodeAnalysis (5.0.0)
   - Microsoft.CodeAnalysis.CSharp.Scripting
   - Microsoft.CodeAnalysis.Scripting;
4. 重启Unity
5. Console可能会报错类似：
   - Assembly 'Assets/Packages/Microsoft.CodeAnalysis.VisualBasic.Workspaces.5.0.0/lib/netstandard2.0/Microsoft.CodeAnalysis.VisualBasic.Workspaces.dll' will not be loaded due to errors:
Reference has errors 'Microsoft.CodeAnalysis.Workspaces'.
   - *解决方法*：直接删掉所有 `*Workspaces*.dll` 文件，或者选中每个 DLL，在 Inspector 里：取消所有平台（勾选 “Any Platform” 去掉，所有 Platform 都不选），让 Unity 不再把它们当插件加载。
1. 无法安装：检查网络；关闭防火墙；手动安装。