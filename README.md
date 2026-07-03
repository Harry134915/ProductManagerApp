# ProductManagerApp

一个基于 **.NET 8 + WPF + MVVM + SQLite** 的商品管理桌面应用，用于演示商品信息的新增、查询、更新、删除、刷新以及基础业务校验流程。

项目采用分层结构组织代码，界面层负责交互展示，业务层负责校验和数据转换，数据访问层负责 SQLite 持久化操作。应用启动时会自动初始化数据库表，首次运行不需要手动准备数据库文件。

## 技术栈

| 类型 | 技术 |
| --- | --- |
| 运行平台 | .NET 8 |
| UI 框架 | WPF |
| 架构模式 | MVVM |
| 数据库 | SQLite |
| 数据访问 | Dapper |
| 依赖注入 | Microsoft.Extensions.DependencyInjection |
| 项目类型 | Windows 桌面应用 |

## 功能特性

- 商品列表展示
- 新增商品
- 更新商品
- 删除商品确认
- 刷新商品列表
- 表单输入校验
- 商品编码不可修改
- 数据库自动初始化
- 数据库访问异常提示
- 成功/错误状态提示动画
- 清空表单并回到新增模式

## 项目结构

```text
ProductManagerApp
├─ ProductManagerApp.sln
├─ README.md
├─ ProgramFlow
│  ├─ 设计文档.md
│  └─ ScreenShot_*.png
└─ ProductManagerApp
   ├─ App.xaml
   ├─ App.xaml.cs
   ├─ ProductManagerApp.csproj
   ├─ Assets
   ├─ BLL
   │  ├─ Exceptions
   │  ├─ Interfaces
   │  ├─ Mappers
   │  ├─ Services
   │  └─ Validators
   ├─ DAL
   │  ├─ Database
   │  ├─ Providers
   │  └─ Repositories
   ├─ DTO
   ├─ Entity
   ├─ Infrastructure
   ├─ ViewModels
   └─ Views
```

## 架构说明

项目整体采用 **MVVM + 分层架构**：

```text
Views
  ↓ 绑定
ViewModels
  ↓ 调用
BLL Services
  ↓ 校验 / 映射
DAL Repositories
  ↓ 执行 SQL
SQLite
```

### Views

负责 WPF 界面展示和用户交互。

主要包含：

- `MainWindow.xaml`
- `ProductFormView.xaml`
- `ProductListView.xaml`
- `DeleteConfirmView.xaml`

界面通过 Binding 绑定 ViewModel 属性和命令，不直接访问数据库。

### ViewModels

负责界面状态、命令和交互流程。

主要包含：

- `MainWindowViewModel`
- `ProductFormViewModel`
- `ProductListViewModel`
- `DeleteConfirmViewModel`

例如新增商品、更新商品、删除确认、刷新列表、清空表单等操作都由 ViewModel 统一协调。

### BLL

业务逻辑层，负责业务规则、校验、DTO 和 Entity 转换。

主要职责：

- 商品基础校验
- 商品编码不可修改校验
- DTO 和 Entity 映射
- 调用 Repository 完成数据持久化
- 抛出业务异常或数据访问异常

### DAL

数据访问层，负责数据库连接、表初始化和 SQL 执行。

主要包含：

- `SqliteProvider`
- `SqliteDatabaseInitializer`
- `ProductRepository`

应用启动时会自动执行建表逻辑，确保 `products` 表存在。

## 数据库说明

数据库文件名：

```text
database.db
```

数据库位置：

```text
AppContext.BaseDirectory/database.db
```

也就是应用运行输出目录下的 `database.db`。

启动时会自动创建表：

```sql
CREATE TABLE IF NOT EXISTS products (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    code TEXT NOT NULL UNIQUE,
    name TEXT NOT NULL,
    price NUMERIC NOT NULL,
    stock INTEGER NOT NULL,
    description TEXT NOT NULL DEFAULT ''
);
```

## 核心流程

### 应用启动

1. WPF 应用启动。
2. `App.xaml.cs` 创建依赖注入容器。
3. 注册数据库、Repository、Service、Validator、ViewModel 和 View。
4. 执行 `IDatabaseInitializer.Initialize()` 初始化数据库表。
5. 创建并显示 `MainWindow`。

### 新增商品

1. 用户填写商品信息。
2. ViewModel 组装 `ProductCreateDto`。
3. Service 将 DTO 映射为 Entity。
4. Validator 校验商品编码、名称、价格、库存等规则。
5. Repository 执行 INSERT SQL。
6. 刷新商品列表并清空表单。

### 更新商品

1. 用户在商品列表中选择一行。
2. 表单自动填充该商品信息。
3. 商品编码进入只读状态。
4. 用户修改名称、价格、库存或描述。
5. Service 校验商品存在且编码未被修改。
6. Repository 执行 UPDATE SQL。
7. 刷新列表并清空表单。

### 删除商品

1. 用户选择商品。
2. 点击删除按钮。
3. 显示删除确认条。
4. 用户确认后执行删除。
5. 刷新列表并清空表单。

## 运行方式

### 环境要求

- Windows
- .NET 8 SDK
- Visual Studio 2022 或支持 .NET 8 的 IDE

### 使用命令行构建

```powershell
dotnet build ProductManagerApp.sln
```

### 使用命令行运行

```powershell
dotnet run --project ProductManagerApp\ProductManagerApp.csproj
```

### 使用 Visual Studio 运行

1. 打开 `ProductManagerApp.sln`
2. 将 `ProductManagerApp` 设置为启动项目
3. 点击运行

## 当前设计约定

- 商品编码用于唯一标识商品，新增后不可修改。
- 商品价格必须大于 0。
- 商品库存不能为负数。
- 商品描述允许为空字符串，但不能只包含空白字符。
- 选中商品列表行后进入编辑模式。
- 点击“清空表单”后回到新增模式。
- 添加、更新、刷新和删除成功使用非阻塞状态提示。
- 删除操作需要用户二次确认。

## 异常处理

项目中主要有两类异常：

- `ProductValidationException`：业务校验异常，例如价格非法、库存非法、编码不可修改。
- `DataAccessException`：数据库访问异常，例如 SQLite 访问失败或 SQL 执行失败。

ViewModel 会捕获这些异常，并显示适合用户理解的提示文案。

## 说明

`Assets/Idol.jpg` 是项目当前窗口图标资源，属于个人设定，保留不做替换。

