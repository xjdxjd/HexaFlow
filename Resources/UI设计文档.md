# UI设计文档

## 文档版本信息
**版本**：v2.0（玉质感重构版）
**设计目标**：创建统一、优雅、高级感的界面，采用**玉质感**冷白/青白/浅碧色调为主，提供沉浸式、安静且高级的聊天体验。整体氛围偏向东方美学中的温润玉石质感，干净、清透、克制。
**设计原则**：
-   整体视觉统一：主背景使用单一玉质感色调，组件采用极浅透明度或微磨砂效果，避免过多层次干扰。
-   高对比、可读性：文本主要使用深灰/墨黑/冷白，确保在浅色背景上极佳可读性。
-   高级感与克制：小圆角、极轻微阴影、磨砂/玉石质感、极简留白。
-   可用性：符合WCAG AA标准、完整键盘导航、动画克制流畅（0.2-0.35s）。
-   **平台**：Windows 10/11，目标分辨率1920x1080+，最小支持1024x768。
-   **工具与框架**：WPF (XAML + C#)，使用MVVM模式；阴影用DropShadowEffect；磨砂效果建议使用BackdropMaterial或自定义Effect。

### 资源组织与主题系统
为了实现样式统一管理、可维护性和主题切换能力，**强烈建议将所有样式、模板、画刷、转换器等资源集中存放在统一的资源字典中**，并采用分层合并的方式组织。
**推荐结构**（项目中的 Resources 文件夹）：

```
Resources/
├── Common.xaml               # 公共颜色、尺寸、CornerRadius、字体等常量
├── Brushes.xaml              # 玉质感主色、半透明白、磨砂背景等
├── ControlStyles.xaml        # 通用控件样式
├── FloatingPanelStyles.xaml  # 浮岛、气泡、对话框专用样式
├── DialogStyles.xaml         # 弹窗统一样式基类
├── Animations.xaml           # 公共动画 Storyboard 与 VisualState
└── ThemeJade.xaml            # 玉质感主题入口（主合并文件）
```

**在 App.xaml 中统一引入**（推荐做法）：

```xml
<Application.Resources>
    <ResourceDictionary>
        <ResourceDictionary.MergedDictionaries>
            <ResourceDictionary Source="Resources/Common.xaml"/>
            <ResourceDictionary Source="Resources/Brushes.xaml"/>
            <ResourceDictionary Source="Resources/ControlStyles.xaml"/>
            <ResourceDictionary Source="Resources/FloatingPanelStyles.xaml"/>
            <!-- ...其他字典 -->
            <ResourceDictionary Source="Resources/Theme.xaml"/>
        </ResourceDictionary.MergedDictionaries>
    </ResourceDictionary>
</Application.Resources>
```
优点：

- 实现全局统一样式，一处修改处处生效
- 便于后续实现多主题切换（只需替换/动态加载不同的 Theme 文件）
- 提高XAML可读性，减少重复代码
- 方便设计器与开发人员协作

后续所有样式定义应尽量使用 x:Key 命名，并优先使用 StaticResource（性能更好），仅在需要运行时动态切换时使用 DynamicResource。


## 整体风格指南

### 主背景（全局唯一）

**主色调**：玉质感冷白青（推荐以下之一，根据实际光感选择最优）  
首选：#F0F5F9（极浅青白玉）  
次选：#F2F8FA（更冷的冰玉感）  
备选：#E8F0F5（略带青绿的翡翠浅调）

**WPF 实现**（推荐方式）：

```xml
<SolidColorBrush x:Key="MainBackgroundBrush" Color="#F0F5F9"/>
<!-- 或使用极浅渐变作为过渡（可选，幅度极小） -->
<LinearGradientBrush x:Key="MainBackgroundBrush" StartPoint="0,0" EndPoint="0,1">
    <GradientStop Color="#F0F5F9" Offset="0.0"/>
    <GradientStop Color="#E8EFF5" Offset="1.0"/>
</LinearGradientBrush>
```
-   **视觉描述**：整体呈现温润、透亮、如上等羊脂白玉或冰种翡翠的质感。安静、内敛、高级，适合长时间阅读与沉浸式对话。

    **应用范围**：主窗口根背景（Window.Background 或最外层 Grid）。


### 组件背景选取规则

| 组件类型                  | 背景色值与透明度/效果            | 说明与适用场景                  |
| ------------------------- | -------------------------------- | ------------------------------- |
| 顶部标题区                | #FFFFFF, Opacity=0.25 ~ 0.35     | 极浅半透明白，玉质轻盈感        |
| 左侧/右侧功能区浮岛       | #FFFFFF, Opacity=0.28 + 轻微磨砂 | 统一半透明白 + 磨砂质感         |
| 中部聊天大浮岛            | #FFFFFF, Opacity=0.32 + 轻微磨砂 | 核心阅读区域，稍强一点透感      |
| 输入浮岛（悬浮）          | #FFFFFF, Opacity=0.38 + 轻微发光 | 突出输入区，微发光边框          |
| 消息气泡（用户）          | #E8F0F5, Opacity=0.9             | 浅青白玉，用户侧稍有区分        |
| 消息气泡（助手）          | #FFFFFF, Opacity=0.92            | 几乎纯白，干净清晰              |
| 按钮（常规/发送）         | #5B8A9B（冷玉青）                | 沉稳青玉色，高级且不刺眼        |
| 列表项/卡片（未选中）     | #FFFFFF, Opacity=0.25            | 极浅统一背景                    |
| 列表项/卡片（选中/hover） | #D4E4ED 或 #5B8A9B（20%透明）    | 选中用浅青，hover用青玉主色淡化 |
| 弹窗基底                  | #FFFFFF, Opacity=0.35 + 轻微磨砂 | 半透明白 + 磨砂，玉质感强       |

-   **文本颜色**：
    -   主要正文：#1F2A44（深冷灰墨）
    -   次要/时间戳：#6B8299（玉青灰）
    -   强调/链接/按钮文字：#FFFFFF（白玉）或 #FFFFFF90（半透白）
-   **阴影**：极轻微 DropShadowEffect Color="#000000" BlurRadius=8~14 ShadowDepth=1~3 Opacity=0.08~0.18
-   **语义色**（基本保留，但更克制）：
    -   成功：#4A937A（玉翠绿）
    -   警告：#C19A5B（玉琥珀黄）
    -   错误：#A14A5C（玉石红）

### 字体与图标

-   **字体**：优先系统字体 **Segoe UI Variable** 或 **Microsoft YaHei UI**（更符合东方审美）
    -   标题：Medium/Bold 18-22pt
    -   正文：Regular 14-15pt
    -   输入：Regular 15pt
-   **图标**：Fluent UI Icons（推荐）或 Line Awesome，轻描线条风格，颜色使用 #5B8A9B 或 #6B8299
-   **间距与圆角**：Padding=16px；Margin=10~12px；CornerRadius=16~20px（更大圆角更玉感）

### 动画与交互

-   **动画类型**（时长缩短，幅度减小）：
    -   淡入/出：Opacity 0→1 (0.25s, EaseOut)
    -   缩放：Scale 1.0→1.03 (hover, 0.2s)
    -   折叠：Width动画 (0.3s, EaseOutCubic)
-   **交互规范**：
    -   Hover：背景Opacity +0.08，极轻微放大，阴影稍加强
    -   Focus：细冷玉青边框 (#5B8A9B)
    -   拖拽：侧边栏边缘支持Resize
    -   快捷键：保持原设计

### 动画实现建议

文档中定义的动画主要使用原生 Storyboard + DoubleAnimation，适合简单场景。但随着动画复杂度增加（组合动画、路径动画、级联效果、性能优化等），**建议考虑引入轻量级第三方动画库**来提升开发效率与动画表现力。

**2026 年推荐的 WPF 动画辅助库**（按易用性与社区活跃度排序）：

1.  **Microsoft.Xaml.Behaviors.Wpf**（官方） → 配合 VisualStateManager 使用，最轻量，推荐作为基础
2.  **MaterialDesignThemes**（MaterialDesignInXamlToolkit） → 内置大量高质量过渡动画与 Ripple 效果，与渐变风格搭配良好
3.  **HandyControl** → 提供大量内置过渡动画、Loading 动画、Notification 等，动画风格现代
4.  **MahApps.Metro** → 提供流畅的 Metro 风格过渡动画，可部分借鉴

**选择建议**：

-   如果只做简单 hover/淡入淡出 → 原生 Storyboard 足够
-   如果需要复杂级联、弹簧、阻尼效果 → 优先考虑 MaterialDesignInXamlToolkit 或 HandyControl
-   性能敏感场景 → 优先使用硬件加速动画（RenderTransform 而非 LayoutTransform）

在项目初期可先使用原生动画，后期视需求逐步引入控件库中的动画资源。

### 交互行为封装（Behaviors）

为了遵循 MVVM 模式、减少代码后置（Code-Behind）中的 UI 逻辑，**强烈建议使用 Behaviors（Microsoft.Xaml.Behaviors.Wpf）** 来封装常见的交互逻辑。

**推荐使用的行为场景**：

-   鼠标进入/离开时的悬浮放大、阴影加深
-   输入框获得焦点时自动上浮/发光
-   自动滚动到聊天列表底部
-   双击折叠/展开侧边栏
-   拖拽排序历史记录
-   按键触发（如 Ctrl+Enter 发送）

**示例**（自动滚到底部行为）：

XML

```
<ItemsControl ItemsSource="{Binding Messages}">
    <i:Interaction.Behaviors>
        <behaviors:ScrollToBottomBehavior/>
    </i:Interaction.Behaviors>
</ItemsControl>
```

**行为类实现**（C#）：

C#

```
public class ScrollToBottomBehavior : Behavior<ItemsControl>
{
    protected override void OnAttached()
    {
        base.OnAttached();
        AssociatedObject.ItemContainerGenerator.StatusChanged += ItemContainerGenerator_StatusChanged;
    }

    private void ItemContainerGenerator_StatusChanged(object sender, EventArgs e)
    {
        if (AssociatedObject.Items.Count > 0)
        {
            var scrollViewer = FindVisualChild<ScrollViewer>(AssociatedObject);
            scrollViewer?.ScrollToEnd();
        }
    }
    // ... 查找 ScrollViewer 的辅助方法
}
```

使用 Behaviors 可大幅减少 MainWindow.xaml.cs 中的事件处理代码，使视图层更纯粹。

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
-   **背景**：统一使用 #F0F5F9 或极浅青白渐变
-   **所有浮岛**：#FFFFFF Opacity 0.25~0.38 + 极轻磨砂感
-   **按钮主色**：#5B8A9B（冷玉青）
-   **消息气泡**：用户侧 #E8F0F5 / 助手侧 #FFFFFF（几乎纯白）
-   **输入框**：更强一点的半透明白 + 冷玉青发光边

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
-   **上部提示条**：高度6px，背景 \#4A937A（绿）。
-   **元素布局**（Vertical StackPanel）：
    -   提示条（Rectangle Height=6）。
    -   图标：勾号图标（CheckCircle，填充\#4A937A）。
    -   正文：简短描述（如"Ollama API 已连接。"）。
-   **样式扩展**：背景 #FFFFFF Opacity=0.3，轻微脉冲动画（GlowEffect 0.2s循环一次）。

### 2. 警告弹窗（Warning Dialog）

-   **触发场景**：潜在风险操作，如参数超出范围、会话即将删除、内存不足警告。
-   **上部提示条**：高度6px，背景 #C19A5B（黄）。
-   **元素布局**（Vertical StackPanel）：
    -   提示条（Rectangle Height=6）。
    -   图标：感叹号图标（WarningTriangle，填充#C19A5B）。
    -   正文：详细说明（如"此操作将永久删除会话，是否继续？"）。
    -   按钮：双按钮 - "确认"（背景 #C19A5B） + "取消"（灰 #6B7280）。
-   **样式扩展**：背景 #FFFFFF Opacity=0.3，图标轻微抖动动画（RotateTransform 0.1s）。

### 3. 错误弹窗（Error Dialog）

-   **触发场景**：失败操作，如API连接失败、模型未找到、生成错误。
-   **上部提示条**：高度6px，背景 #A14A5C（红）。
-   **元素布局**（Vertical StackPanel）：
    -   提示条（Rectangle Height=6）。
    -   图标：叉号图标（ErrorCross，填充#A14A5C）。
    -   正文：错误描述 + 建议（如"Ollama 服务未运行。请检查并重试。"）。
    -   按钮：单一"确认"Button（背景 #C19A5B）；可选"重试"Button。
-   **样式扩展**：背景 #FFFFFF Opacity=0.3，轻微闪烁动画（Opacity 0.8-1.0，0.3s）。
-   **统一实现建议**：定义基类DialogWindow（继承Window），子类SuccessDialog/WarningDialog/ErrorDialog。资源字典中定义提示条Brush。所有弹窗支持自定义消息注入（via Constructor参数）。



## 资源组织与主题系统

为了实现样式统一管理、可维护性和主题切换能力，**强烈建议将所有样式、模板、画刷、转换器等资源集中存放在统一的资源字典中**，并采用分层合并的方式组织。

**推荐结构**（项目中的 Resources 文件夹）：

```text

Resources/ 
├── Common.xaml               # 公共颜色、尺寸、CornerRadius、字体等常量 
├── Brushes.xaml              # 所有渐变、纯色、半透明画刷定义 
├── ControlStyles.xaml        # 通用控件样式（Button、TextBox、ListView等） 
├── FloatingPanelStyles.xaml  # 浮岛、气泡、对话框专用样式 
├── DialogStyles.xaml         # 各种弹窗统一样式基类
├── Animations.xaml           # 公共动画 Storyboard 与 VisualState 
└── Theme.xaml                # 主主题入口，合并以上所有字典
```



**在 App.xaml 中统一引入**（推荐做法）：

```xml
<Application.Resources>
    <ResourceDictionary>
        <ResourceDictionary.MergedDictionaries>
            <ResourceDictionary Source="Resources/Common.xaml"/>
            <ResourceDictionary Source="Resources/Brushes.xaml"/>
            <ResourceDictionary Source="Resources/ControlStyles.xaml"/>
            <ResourceDictionary Source="Resources/FloatingPanelStyles.xaml"/>
            <!-- ...其他字典 -->
            <ResourceDictionary Source="Resources/Theme.xaml"/>
        </ResourceDictionary.MergedDictionaries>
    </ResourceDictionary>
</Application.Resources>
```

后续所有样式定义应尽量使用 x:Key 命名，并优先使用 StaticResource（性能更好），仅在需要运行时动态切换时使用 DynamicResource。




## 组件库（Reusable Styles）

-   **FloatingPanelStyle**（聊天/侧边栏浮岛）：

    XML

    ```
    <Style x:Key="FloatingPanelJadeStyle" TargetType="Border">
        <Setter Property="Background" Value="#FFFFFF"/>
        <Setter Property="Opacity" Value="0.32"/>
        <Setter Property="CornerRadius" Value="18"/>
        <Setter Property="Padding" Value="16"/>
        <Setter Property="Effect">
            <Setter.Value>
                <DropShadowEffect BlurRadius="10" ShadowDepth="2" Opacity="0.12" Color="Black"/>
            </Setter.Value>
        </Setter>
    </Style>
    
    <!-- 主按钮示例 -->
    <Style x:Key="JadeButtonStyle" TargetType="Button">
        <Setter Property="Background" Value="#5B8A9B"/>
        <Setter Property="Foreground" Value="White"/>
        <Setter Property="BorderThickness" Value="0"/>
        <Setter Property="Padding" Value="16,10"/>
        <Setter Property="CornerRadius" Value="16"/>
        <!-- ... hover/pressed 状态使用更浅的 #6B98A8 等 -->
    </Style>
    ```



## 控件模板（ControlTemplate）使用规范

为确保所有自定义外观控件（如按钮、气泡、输入框等）具有一致的行为与视觉结构，**建议对需要大幅改变外观的控件都定义完整的 ControlTemplate**，而不是只覆盖部分属性。

**推荐实践**： 1. 保留原生控件的核心功能（FocusVisual、键盘交互等） 2. 使用命名约定：`{控件类型}Template.{风格名}` 如 `ButtonTemplate.GradientRed` 3. 在模板中尽量使用 **ContentPresenter**、**ItemsPresenter** 等占位符，保持内容可替换性 4. 所有交互状态（Normal / MouseOver / Pressed / Disabled / Focused）都应在模板内使用 **VisualStateManager** 或 **Trigger** 完整定义

**示例**（发送按钮模板片段）：

```xml
<ItemsControl ItemsSource="{Binding Messages}">
    <i:Interaction.Behaviors>
        <behaviors:ScrollToBottomBehavior/>
    </i:Interaction.Behaviors>
</ItemsControl>
```

行为类实现（C#）：

```c#
public class ScrollToBottomBehavior : Behavior<ItemsControl>
{
    protected override void OnAttached()
    {
        base.OnAttached();
        AssociatedObject.ItemContainerGenerator.StatusChanged += ItemContainerGenerator_StatusChanged;
    }

    private void ItemContainerGenerator_StatusChanged(object sender, EventArgs e)
    {
        if (AssociatedObject.Items.Count > 0)
        {
            var scrollViewer = FindVisualChild<ScrollViewer>(AssociatedObject);
            scrollViewer?.ScrollToEnd();
        }
    }
    // ... 查找 ScrollViewer 的辅助方法
}
```

使用 Behaviors 可大幅减少 MainWindow.xaml.cs 中的事件处理代码，使视图层更纯粹。
