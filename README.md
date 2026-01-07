# HexaFlow

![HexaFlow Logo](Resources/app.ico)

HexaFlow是一款基于WPF的现代化AI聊天应用，集成了本地大语言模型，提供了美观的界面和流畅的用户体验。

## 功能特点

- 🤖 **本地AI模型集成**：支持Ollama本地部署的多种大语言模型
- 💬 **对话管理**：支持创建、保存和管理多个对话会话
- 📝 **历史记录**：自动保存对话历史，随时回顾和继续之前的对话
- 🎨 **现代UI设计**：采用现代化的界面设计，支持深色/浅色主题
- ⚡ **实时响应**：流式输出AI回复，提供实时对话体验
- 🛠️ **模型管理**：方便的模型选择和参数配置
- 📱 **响应式布局**：支持窗口大小调整和侧边栏折叠

## 系统要求

- Windows 10/11
- .NET 8.0 或更高版本
- [Ollama](https://ollama.ai/) (用于运行本地AI模型)

## 安装说明

### 1. 安装Ollama

请访问[Ollama官网](https://ollama.ai/)下载并安装Ollama。安装后，确保Ollama服务正在运行。

### 2. 下载AI模型

使用以下命令下载一个AI模型（以Llama 3为例）：

```bash
ollama pull llama3
```

### 3. 运行HexaFlow

1. 从[Releases](https://github.com/xjdxjd/HexaFlow/releases)页面下载最新版本的HexaFlow
2. 解压并运行`HexaFlow.exe`

## 使用指南

### 开始新对话

1. 点击左侧边栏的"新建会话"按钮
2. 在底部输入框中输入您的问题
3. 点击发送按钮或按Enter键发送消息

### 管理会话

- **查看历史会话**：左侧边栏显示所有历史会话列表
- **切换会话**：点击列表中的任意会话即可加载该会话的聊天记录
- **删除会话**：将鼠标悬停在会话项上，点击右侧出现的"×"按钮删除会话

### 模型管理

1. 点击左侧边栏顶部的下拉框选择不同的AI模型
2. 点击"模型管理"按钮可以查看已安装的模型详情
3. 通过右侧配置面板调整模型参数（温度、Top P、Top K等）

## 开发说明

### 构建要求

- Visual Studio 2022 或 Visual Studio Code
- .NET 8.0 SDK

### 构建步骤

1. 克隆仓库
```bash
git clone https://github.com/xjdxjd/HexaFlow.git
cd HexaFlow
```

2. 还原NuGet包
```bash
dotnet restore
```

3. 构建项目
```bash
dotnet build
```

4. 运行项目
```bash
dotnet run
```

### 项目结构

```
HexaFlow/
├── Resources/           # 资源文件（图标等）
├── Services/            # 服务类（如ChatHistoryService）
├── Views/               # 视图窗口
├── MainWindow.xaml      # 主窗口界面
├── MainWindow.xaml.cs   # 主窗口代码
├── App.xaml             # 应用程序资源
└── App.xaml.cs          # 应用程序入口
```

## 贡献指南

欢迎提交Issue和Pull Request来帮助改进HexaFlow！

### 开发流程

1. Fork本仓库
2. 创建特性分支 (`git checkout -b feature/AmazingFeature`)
3. 提交更改 (`git commit -m 'Add some AmazingFeature'`)
4. 推送到分支 (`git push origin feature/AmazingFeature`)
5. 提交Pull Request

## 许可证

本项目采用MIT许可证 - 查看[LICENSE](LICENSE)文件了解详情。

## 致谢

- [OllamaSharp](https://github.com/awaescher/OllamaSharp) - 用于与Ollama API交互
- [iNKORE.UI.WPF.Modern](https://github.com/iNKORE-UI/WPF.Modern) - 提供现代化UI控件
- [MdXaml](https://github.com/whistyun/MdXaml) - Markdown渲染支持

## 更新日志

### v1.0.0 (2024-06-18)
- 初始版本发布
- 基本聊天功能
- 模型选择和参数配置
- SQLite数据库保存对话历史
- 会话管理功能
