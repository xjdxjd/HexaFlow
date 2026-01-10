# UI设计文档

## 文档版本信息
-   **设计目标**：创建统一、优雅的科技感界面，以蓝紫-红橙渐变为主调，提供沉浸式聊天体验。主背景渐变为深蓝到亮红的动态过渡，组件采用半透明白色叠加以显露渐变。
-   **设计原则**：
    -   整体视觉统一：主背景使用指定渐变，组件背景统一为纯白色（Opacity=0.3），避免多层渐变冲突。
    -   高对比、可读性：文本使用白色/浅灰，确保在半透明背景上清晰。
    -   现代感：圆角、阴影、悬浮、折叠机制保留。
    -   可用性：高对比度（WCAG AA标准）、键盘导航支持、动画流畅（0.2-0.4s时长）。
    -   **平台**：Windows 10/11，目标分辨率1920x1080+，最小支持1024x768。
-   **工具与框架**：WPF (XAML + C#)，使用MVVM模式；渐变用LinearGradientBrush；动画用Storyboard；阴影用DropShadowEffect。

## 整体风格指南

### 主背景渐变（全局唯一）

-   **CSS 等效**：background-image: linear-gradient(45deg, #2a299c, #842272, #b41b49, #dd1019);

-   **WPF 等效实现**（XAML）：

    XML

    ```
    <LinearGradientBrush StartPoint="0,0" EndPoint="1,1" Opacity="1">
        <GradientStop Color="#2a299c" Offset="0.0" />
        <GradientStop Color="#842272" Offset="0.33" />
        <GradientStop Color="#b41b49" Offset="0.66" />
        <GradientStop Color="#dd1019" Offset="1.0" />
    </LinearGradientBrush>
    ```

-   **视觉描述**：45度倾斜，从左上深蓝（冷静、科技感）平滑过渡到右下亮红（活力、警示感）。整体给人深邃到激情的科技氛围，适合AI聊天应用。

-   **应用范围**：仅用于**主窗口根背景**（Window.Background 或最外层 Grid/Rectangle）。所有组件背景叠加在渐变之上。

### 组件背景选取规则

所有浮岛、面板、对话框、按钮等组件的背景**统一为纯白色（#FFFFFF, Opacity=0.3）**，以半透明方式显露主渐变，避免视觉杂乱。特殊强调色（如按钮hover、弹窗提示条）从渐变中提取：

| 组件类型                  | 背景色值与透明度      | 说明与适用场景                     |
| ------------------------- | --------------------- | ---------------------------------- |
| 顶部标题区                | #FFFFFF, Opacity=0.3  | 半透明白色，叠加渐变，标题区轻盈感 |
| 左侧功能区各部分浮岛      | #FFFFFF, Opacity=0.3  | 统一半透明，显露渐变，协调整体     |
| 中部聊天大浮岛            | #FFFFFF, Opacity=0.3  | 半透明白色，聊天区沉浸             |
| 输入浮岛（悬浮）          | #FFFFFF, Opacity=0.3  | 半透明白色，突出悬浮               |
| 右侧功能区各部分浮岛      | #FFFFFF, Opacity=0.3  | 统一半透明，区分左右侧             |
| 消息气泡（用户/助手）     | #FFFFFF, Opacity=0.5  | 稍高透明，气泡更突出               |
| 按钮（常规/发送）         | #dd1019（渐变末端红） | 纯色高亮，易点击                   |
| 列表项/卡片（未选中）     | #FFFFFF, Opacity=0.3  | 背景统一                           |
| 列表项/卡片（选中/hover） | #b41b49（渐变中间红） | 纯色高亮                           |
| 弹窗基底                  | #FFFFFF, Opacity=0.3  | 半透明白色 + 语义色提示条          |

-   **文本颜色**：在半透明背景上用 #FFFFFF / #F3F4F6（白/浅灰）；hover/强调用渐变提取色如 #dd1019。
-   **阴影**：所有浮岛统一使用 DropShadowEffect（Color="#000000" BlurRadius=12~20 ShadowDepth=4 Opacity=0.35~0.5）。
-   **语义色（弹窗等）**：成功 #22C55E（绿）；警告 #FBBF24（黄）；错误 #EF4444（红）。

### 字体与图标

-   **字体**：系统字体Segoe UI Variable；标题：Bold 18-24pt；正文：Regular 14pt；输入：14pt。
-   **图标**：Fluent UI Icons 或 Material Icons（渐变填充或纯 #dd1019 红）；大小：24x24px（按钮）、16x16px（列表）。
-   **间距与圆角**：Padding=12px；Margin=8px；CornerRadius=12px（浮岛）。

### 动画与交互

-   **动画类型**：
    -   淡入/出：Opacity from 0 to 1 (0.3s, EaseInOut)。
    -   缩放：ScaleTransform 1.0 to 1.05 (hover, 0.2s)。
    -   折叠：Width动画 (0.35s, EaseOut)。
-   **交互规范**：
    -   Hover：背景Opacity +0.1、阴影加深。
    -   Focus：红边框 (#dd1019)。
    -   拖拽：侧边栏边缘支持Resize（Cursor=SizeWE）。
    -   快捷键：Ctrl+←/→ 折叠左侧/右侧；Enter发送消息。

### 响应式设计

-   **窗口最小**：1200x800px（低于时默认折叠侧边）。
-   **自适应**：Grid * sizing；聊天区始终填满垂直空间。
-   **折叠机制**（左右侧）：
    -   折叠宽度：48-60px（窄条显示箭头图标+摘要，如模型名/参数值）。
    -   折叠后：中部聊天浮岛横向撑满窗口（*列扩展）。
    -   展开后：中部回归正常尺寸（侧边固定宽度恢复）。
    -   触发：点击箭头、双击窄条、快捷键。
    -   状态持久化：保存到配置文件。

## 主界面布局（MainWindow）

-   **总体结构**：单窗口，Grid布局（Rows: 1标题 + 1主内容；Columns: 左侧固定280-340px + 中部* + 右侧固定320-380px）。
-   **背景**：全窗上述45deg渐变（唯一渐变层），其他所有组件为纯白色Opacity=0.3 + 阴影浮岛。
-   **详细区域**：

### 1. 顶部标题区（Grid Row 0，全宽，固定高度48-56px）

-   **样式**：背景 #FFFFFF Opacity=0.3（半透明白色），底部1px渐变分隔线。
-   **元素布局**（Horizontal StackPanel）：
    -   左侧：应用图标（24x24px，渐变填充或 #dd1019 红）。
    -   中间：TextBlock "Ollama Client"（Bold 20pt）。
    -   右侧：TextBlock "当前模型: [SelectedModel]"（Regular 16pt，点击弹出模型选择Popup）。
    -   窗口控制按钮（Minimize/Maximize/Close，自定义Style：hover #dd1019）。
-   **交互**：拖拽移动窗口；双击最大化。

### 2. 左侧功能区（Grid Column 0，全高，可折叠）

-   **样式**：背景 #FFFFFF Opacity=0.3，垂直Grid分3部分，每个部分独立浮岛（Border + Shadow）。
-   **上部（高度120px）**：本地模型下拉选择浮岛。
    -   ComboBox（ItemsSource绑定模型列表，ItemTemplate: 名称+大小）。
    -   右侧刷新Button（图标：循环箭头）。
    -   加载动画：ProgressRing。
-   **中部（高度*，自适应）**：历史记录列表浮岛（可滚动）。
    -   ListView（ItemsSource绑定会话列表，ItemTemplate: 卡片 - 标题TextBlock + 预览TextBlock + 时间Label）。
    -   操作：顶部工具栏（新建Button + 搜索TextBox）；右键ContextMenu（修改名称、删除）；CheckBox批量删除。
    -   交互：点击切换会话（加载到聊天区）；拖拽排序。
-   **下部（高度80px）**：入口浮岛。
    -   Horizontal StackPanel：系统设置Button（齿轮图标 → 打开设置对话框）；模型管理Button（列表图标 → 打开模型管理对话框）。
-   **折叠状态**：窄条（宽度56px），显示垂直图标列（模型图标 + 会话数Badge + 设置/管理快捷图标）；点击箭头展开。

### 3. 中部聊天区（Grid Column 1，宽度*，全高）

-   **样式**：超大浮岛（Border Margin=12, CornerRadius=16, Shadow Blur=12），背景 #FFFFFF Opacity=0.3，占据整个列高度（从标题下到窗口底）。
-   **聊天记录区**：内部ScrollViewer + ItemsControl（VerticalAlignment=Stretch，填满浮岛空间）。
    -   消息模板：气泡Border（用户右对齐，背景 #FFFFFF Opacity=0.5；助手左对齐，支持Markdown渲染 via RichTextBox）。
    -   每气泡：文本 + 时间戳 + 操作图标（hover显示：复制/编辑/重新生成）。
    -   加载指示：生成中渐变Spinner或打字机动画（TextBlock逐字追加）。
    -   交互：鼠标滚轮/触屏滚动；自动滚到底部（OnNewMessage）。
-   **输入浮岛**：独立小浮岛（ZIndex=10，VerticalAlignment=Bottom，Margin=40,0,40,20），背景 #FFFFFF Opacity=0.3。
    -   宽度：自适应聊天区80-90%。
    -   高度：自适应（单行60px，多行max 200px）。
    -   元素：Grid - TextBox（多行，TextWrapping=Wrap，Scroll if overflow）占* + Button（"发送" 或箭头图标，背景 #dd1019）占Auto。
    -   样式：更强阴影（Blur=20），发光边框（GlowBrush=#b41b49）。
    -   交互：Ctrl+Enter发送；焦点时轻微上浮动画（TranslateY=-4px）。
-   **响应式**：侧边折叠时，此区横向扩展；内容少时消息从顶开始。

### 4. 右侧功能区（Grid Column 2，全高，可折叠）

-   **样式**：背景 #FFFFFF Opacity=0.3，垂直Grid分2部分，每个部分独立浮岛。
-   **上部（高度40%）**：参数快捷设置浮岛。
    -   Vertical StackPanel：每个参数一行（Label + Slider/TextBox Combo）。
    -   参数：Temperature (Slider 0-2)，Seed (TextBox int)，Top_P (Slider 0-1)，Top_K (Slider 1-100)。
    -   底部"应用"Button（背景 #dd1019）。
    -   交互：实时预览（绑定ViewModel），ToolTip解释每个参数。
-   **下部（高度60%）**：系统提示词列表浮岛（可滚动）。
    -   ListView（ItemsSource绑定提示词列表，ItemTemplate: 卡片 - 标题 + 预览TextBlock）。
    -   操作：顶部工具栏（新建/搜索Button）；右键ContextMenu（选择/更新/删除/批量）。
    -   交互：点击选择（应用到当前对话）；双击编辑（弹出TextBox）。
-   **折叠状态**：窄条（宽度56px），显示垂直摘要（参数值Badge + 当前提示词缩写）；点击箭头展开。

## 辅助界面

### 1. 设置对话框（Modal Window，600x500px，居中弹出）

-   **触发**：左侧设置入口。
-   **布局**：TabControl（TabItem背景 #FFFFFF Opacity=0.3）。
    -   **连接Tab**：浮岛分组 - API URL TextBox + 测试Button + 重连CheckBox。
    -   **通用Tab**：主题下拉 + 通知开关 + 快捷键列表。
    -   **附加Tab**：日志查看Button + 更新检查。
-   **底部**：保存/取消Button（渐变Style）。
-   **样式**：窗口背景 #FFFFFF Opacity=0.3，弹出动画（Scale from 0.8 to 1.0）。

### 2. 模型管理对话框（Modal Window，700x600px，居中弹出）

-   **触发**：左侧模型管理入口。
-   **布局**：上部搜索TextBox + Pull新模型Button；下部ListView（模型卡片: 名称 + 大小 + 删除Button）。
-   **交互**：Pull调用Ollama API；删除确认Dialog。
-   **样式**：类似设置对话框，背景 #FFFFFF Opacity=0.3。

## 弹窗界面风格设计（统一规范）

-   **概述**：所有弹窗采用统一的模态设计（Modal Dialog），背景 #FFFFFF Opacity=0.3，以浮岛风格呈现，确保与整体一致。弹窗用于反馈操作结果，如连接成功、参数警告或API错误。共同特点：居中弹出（Owner=MainWindow），CornerRadius=12px，阴影效果，弹出位置为界面中上部（Top=标题栏高度 + 100px, HorizontalAlignment=Center）。动画：从界面中央渐入（Opacity 0 to 1）并上浮到标题栏下方（TranslateY from 200px to 0px, 0.4s EaseOut）。尺寸固定（400x200px，可根据内容自适应高度）。支持键盘Esc关闭。
-   **共同元素**：
    -   **上部提示条**：高度固定6px，纯色填充（根据类型），无文本，仅视觉指示。
    -   **消息正文**：TextBlock（Regular 14pt，Padding=12px，TextWrapping=Wrap）。
    -   **图标**：左侧24x24px图标（渐变填充）。
    -   **按钮区**：底部Horizontal StackPanel，按钮背景 #dd1019（Padding=8px）。
    -   **动画**：弹出时渐入 + 上浮（结合 DoubleAnimation for TranslateY）。

### 1. 成功弹窗（Success Dialog）

-   **触发场景**：操作成功，如连接测试通过、模型加载完成、会话保存。
-   **上部提示条**：高度6px，背景 #22C55E（绿）。
-   **元素布局**（Vertical StackPanel）：
    -   提示条（Rectangle Height=6）。
    -   图标：勾号图标（CheckCircle，填充#22C55E）。
    -   正文：简短描述（如"Ollama API 已连接。"）。
    -   按钮：单一"确认"Button（背景 #dd1019）。
-   **样式扩展**：背景 #FFFFFF Opacity=0.3，轻微脉冲动画（GlowEffect 0.2s循环一次）。

### 2. 警告弹窗（Warning Dialog）

-   **触发场景**：潜在风险操作，如参数超出范围、会话即将删除、内存不足警告。
-   **上部提示条**：高度6px，背景 #FBBF24（黄）。
-   **元素布局**（Vertical StackPanel）：
    -   提示条（Rectangle Height=6）。
    -   图标：感叹号图标（WarningTriangle，填充#FBBF24）。
    -   正文：详细说明（如"此操作将永久删除会话，是否继续？"）。
    -   按钮：双按钮 - "确认"（背景 #dd1019） + "取消"（灰 #6B7280）。
-   **样式扩展**：背景 #FFFFFF Opacity=0.3，图标轻微抖动动画（RotateTransform 0.1s）。

### 3. 错误弹窗（Error Dialog）

-   **触发场景**：失败操作，如API连接失败、模型未找到、生成错误。
-   **上部提示条**：高度6px，背景 #EF4444（红）。
-   **元素布局**（Vertical StackPanel）：
    -   提示条（Rectangle Height=6）。
    -   图标：叉号图标（ErrorCross，填充#EF4444）。
    -   正文：错误描述 + 建议（如"Ollama 服务未运行。请检查并重试。"）。
    -   按钮：单一"确认"Button（背景 #dd1019）；可选"重试"Button。
-   **样式扩展**：背景 #FFFFFF Opacity=0.3，轻微闪烁动画（Opacity 0.8-1.0，0.3s）。
-   **统一实现建议**：定义基类DialogWindow（继承Window），子类SuccessDialog/WarningDialog/ErrorDialog。资源字典中定义提示条Brush。所有弹窗支持自定义消息注入（via Constructor参数）。

## 组件库（Reusable Styles）

-   **FloatingPanelStyle**（聊天/侧边栏浮岛）：

    XML

    ```
    <Style x:Key="FloatingPanelStyle" TargetType="Border">
        <Setter Property="Background" Value="#FFFFFF" />
        <Setter Property="Opacity" Value="0.3" />
        <Setter Property="CornerRadius" Value="12" />
        <Setter Property="Padding" Value="12" />
        <Setter Property="Effect">
            <Setter.Value>
                <DropShadowEffect BlurRadius="16" ShadowDepth="6" Opacity="0.45" Color="Black"/>
            </Setter.Value>
        </Setter>
    </Style>
    ```

-   **InputFloatingStyle**（输入框专用）：Opacity=0.3

-   **DialogBaseStyle**（弹窗）：Window Style - NoResize, WindowStyle=None, AllowsTransparency=True, Background=Transparent；内部Border为浮岛 (Opacity=0.3)。

-   **ButtonStyle**：背景 #dd1019，Hover: Opacity +0.1。