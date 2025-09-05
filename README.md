# AutoLaunchTestTool

AutoLaunchTestTool 是 [AutoLaunch](https://github.com/Linlccc/AutoLaunch) 项目的测试工具，主要用于验证和演示 AutoLaunch 库的自动启动相关功能。

本项目基于 Avalonia UI 构建，提供跨平台的图形界面，方便开发者在 Windows、macOS 和 Linux 上测试 AutoLaunch 的各项能力。

## 主要功能

- 展示 AutoLaunch API 的使用方法
- 统一异常处理和权限提示

## 项目拉取

```sh
git clone https://github.com/Linlccc/AutoLaunchTestTool.git
cd AutoLaunchTestTool
git submodule update --init --recursive
```

## 发布

> 以下指令均默认在AutoLaunchTestTool项目根目录下执行，而不是仓库根目录\
>
> 从仓库根目录进入项目目录： `cd src/AutoLaunchTestTool`

### Windows

```bash
# aot
dotnet publish -c Release -r win-x64 -o ./bin/win-x64_Aot
# 自包含
dotnet publish -c Release -p:PublishAot=false --self-contained -r win-x64 -o ./bin/win-x64
```

### Linux

```bash
# aot
dotnet publish -c Release -r linux-x64 -o ./bin/linux-x64_Aot
# 自包含
dotnet publish -c Release -p:PublishAot=false --self-contained -r linux-x64 -o ./bin/linux-x64
# arm64 自包含
dotnet publish -c Release -p:PublishAot=false --self-contained -r linux-arm64 -o ./bin/linux-arm64
```

### macOS

```bash
dotnet publish -c Release -r osx-arm64 -o ./bin/osx-arm64_Aot
dotnet publish -c Release -p:PublishAot=false --self-contained -r osx-arm64 -o ./bin/osx-arm64
```

## 依赖

- [AutoLaunch](https://github.com/Linlccc/AutoLaunch)
- [Avalonia UI](https://github.com/AvaloniaUI/Avalonia)
- [ReactiveUI](https://github.com/reactiveui/reactiveui)

## 许可证

本项目遵循 [MIT](LICENSE) 许可证。
