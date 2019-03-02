USE [TradingServer]
GO
/****** Object:  Table [dbo].[BarsDaily]    Script Date: 2017-07-02 21:46:14 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[BarsDaily](
	[Timestamp] [date] NOT NULL,
	[OpenBid] [decimal](18, 5) NOT NULL,
	[OpenAsk] [decimal](18, 5) NOT NULL,
	[HighBid] [decimal](18, 5) NOT NULL,
	[HighAsk] [decimal](18, 5) NOT NULL,
	[LowBid] [decimal](18, 5) NOT NULL,
	[LowAsk] [decimal](18, 5) NOT NULL,
	[CloseBid] [decimal](18, 5) NOT NULL,
	[CloseAsk] [decimal](18, 5) NOT NULL,
	[VolumeBid] [real] NOT NULL,
	[VolumeAsk] [real] NOT NULL,
	[Symbol] [nvarchar](24) NOT NULL,
	[DataFeed] [nvarchar](32) NOT NULL,
 CONSTRAINT [PK_BarsDaily] PRIMARY KEY CLUSTERED 
(
	[Timestamp] ASC,
	[Symbol] ASC,
	[DataFeed] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[BarsMinute]    Script Date: 2017-07-02 21:46:14 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[BarsMinute](
	[Timestamp] [smalldatetime] NOT NULL,
	[OpenBid] [decimal](18, 5) NOT NULL,
	[OpenAsk] [decimal](18, 5) NOT NULL,
	[HighBid] [decimal](18, 5) NOT NULL,
	[HighAsk] [decimal](18, 5) NOT NULL,
	[LowBid] [decimal](18, 5) NOT NULL,
	[LowAsk] [decimal](18, 5) NOT NULL,
	[CloseBid] [decimal](18, 5) NOT NULL,
	[CloseAsk] [decimal](18, 5) NOT NULL,
	[VolumeBid] [real] NOT NULL,
	[VolumeAsk] [real] NOT NULL,
	[Symbol] [nvarchar](24) NOT NULL,
	[DataFeed] [nvarchar](32) NOT NULL,
 CONSTRAINT [PK_BarsMinute] PRIMARY KEY CLUSTERED 
(
	[Timestamp] ASC,
	[Symbol] ASC,
	[DataFeed] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[BarsMinute_Archive]    Script Date: 2017-07-02 21:46:14 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[BarsMinute_Archive](
	[Timestamp] [smalldatetime] NOT NULL,
	[OpenBid] [decimal](18, 5) NOT NULL,
	[OpenAsk] [decimal](18, 5) NOT NULL,
	[HighBid] [decimal](18, 5) NOT NULL,
	[HighAsk] [decimal](18, 5) NOT NULL,
	[LowBid] [decimal](18, 5) NOT NULL,
	[LowAsk] [decimal](18, 5) NOT NULL,
	[CloseBid] [decimal](18, 5) NOT NULL,
	[CloseAsk] [decimal](18, 5) NOT NULL,
	[VolumeBid] [real] NOT NULL,
	[VolumeAsk] [real] NOT NULL,
	[Symbol] [nvarchar](24) NOT NULL,
	[DataFeed] [nvarchar](32) NOT NULL,
 CONSTRAINT [PK_BarsMinute_Archive] PRIMARY KEY CLUSTERED 
(
	[Timestamp] ASC,
	[Symbol] ASC,
	[DataFeed] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[Orders]    Script Date: 2017-07-02 21:46:14 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Orders](
	[ID] [int] IDENTITY(1,1) NOT NULL,
	[OrderId] [nvarchar](32) NOT NULL,
	[BrokerID] [nvarchar](32) NOT NULL,
	[SignalID] [nvarchar](50) NULL,
	[BrokerName] [nvarchar](32) NOT NULL,
	[DataFeed] [nvarchar](32) NOT NULL,
	[UserName] [nvarchar](50) NOT NULL,
	[AccountId] [nvarchar](50) NOT NULL,
	[Symbol] [nvarchar](24) NOT NULL,
	[Price] [decimal](18, 8) NOT NULL,
	[Quantity] [decimal](18, 5) NOT NULL,
	[ExecutedQuantity] [decimal](18, 5) NOT NULL,
	[Status] [nvarchar](8) NOT NULL,
	[Type] [nvarchar](8) NOT NULL,
	[TIF] [nvarchar](3) NOT NULL,
	[Date] [datetime] NOT NULL,
	[PlacedDate] [datetime] NULL,
	[FilledDate] [datetime] NULL,
	[DataBaseEntry] [datetime] DEFAULT GETUTCDATE() NOT NULL,
	[OpeningQuantity] [decimal](18, 5) NOT NULL,
	[ClosingQuantity] [decimal](18, 5) NOT NULL,
	[Origin] [nvarchar](128) NULL,	
PRIMARY KEY CLUSTERED 
(
	[ID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[PortfolioAccounts]    Script Date: 2017-07-02 21:46:14 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[PortfolioAccounts](
	[ID] [int] IDENTITY(1,1) NOT NULL,
	[Portfolio_ID] [int] NOT NULL,
	[Name] [nvarchar](100) NOT NULL,
	[BrokerName] [nvarchar](100) NOT NULL,
	[UserName] [nvarchar](100) NOT NULL,
	[Account] [nvarchar](100) NOT NULL,
PRIMARY KEY CLUSTERED 
(
	[ID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[Portfolios]    Script Date: 2017-07-02 21:46:14 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Portfolios](
	[ID] [int] IDENTITY(1,1) NOT NULL,
	[Currency] [nvarchar](3) NOT NULL,
	[User] [nvarchar](50) NOT NULL,
	[Name] [nvarchar](100) NOT NULL,
PRIMARY KEY CLUSTERED 
(
	[ID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[PortfolioStrategies]    Script Date: 2017-07-02 21:46:14 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[PortfolioStrategies](
	[ID] [int] IDENTITY(1,1) NOT NULL,
	[Portfolio_ID] [int] NOT NULL,
	[StrategyName] [nvarchar](100) NOT NULL,
	[StrategySignals] [nvarchar](max) NULL,
	[StrategyDataFeeds] [nvarchar](500) NULL,
	[ExposedBalance] [decimal](18, 2) NOT NULL,
 CONSTRAINT [PK_PortfolioStrategies] PRIMARY KEY CLUSTERED 
(
	[ID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO
/****** Object:  Table [dbo].[Securities]    Script Date: 2017-07-02 21:46:14 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Securities](
	[Id] [int] NOT NULL,
	[Symbol] [nvarchar](24) NOT NULL,
	[Name] [nvarchar](64) NOT NULL,
	[DataFeed] [nvarchar](32) NOT NULL,
	[PriceIncrement] [decimal](18, 8) NOT NULL,
	[QtyIncrement] [decimal](18, 5) NOT NULL,
	[Digit] [int] NOT NULL,
	[AssetClass] [nvarchar](64) NOT NULL,
	[BaseCurrency] [nvarchar](12) NOT NULL,
	[UnitOfMeasure] [nvarchar](64) NOT NULL,
	[MarginRate] [decimal](18, 5) NOT NULL,
	[MaxPosition] [decimal](18, 5) NOT NULL,
	[UnitPrice] [decimal](18, 5) NOT NULL,
	[ContractSize] [decimal](18, 5) NOT NULL,
	[MarketOpen] [time](7) NOT NULL,
	[MarketClose] [time](7) NOT NULL,
 CONSTRAINT [PK_SecuritiesID] PRIMARY KEY CLUSTERED 
(
	[Symbol] ASC,
	[DataFeed] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[SimulatedExchangeAccounts]    Script Date: 2017-07-02 21:46:14 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[SimulatedExchangeAccounts](
	[UserName] [nvarchar](50) NOT NULL,
	[AccountName] [nvarchar](100) NOT NULL,
	[Currency] [nvarchar](3) NOT NULL,
	[Balance] [decimal](18, 5) NOT NULL,
	[Profit] [decimal](18, 5) NOT NULL,
 CONSTRAINT [PK_SimulatedExchangeAccounts] PRIMARY KEY CLUSTERED 
(
	[UserName] ASC,
	[AccountName] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]
GO

/****** Object:  Table [dbo].[SimulatedExchangePositions]    Script Date: 2017-07-02 21:46:14 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[SimulatedExchangePositions](
	[Symbol] [nvarchar](24) NOT NULL,
	[UserName] [nvarchar](50) NOT NULL,
	[Account] [nvarchar](100) NOT NULL,
	[BrokerName] [nvarchar](32) NOT NULL,
	[Created] [datetime2](7) NOT NULL,
	[Quantity] [decimal](18, 5) NOT NULL,
	[Price] [decimal](18, 5) NOT NULL,
	[Profit] [decimal](18, 5) NOT NULL,
 CONSTRAINT [PK_SimulatedExchangePositions] PRIMARY KEY CLUSTERED 
(
	[Symbol] ASC,
	[UserName] ASC,
	[Account] ASC,
	[BrokerName] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[SimulatedMarginAccounts]    Script Date: 2017-07-02 21:46:14 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[SimulatedMarginAccounts](
	[UserName] [nvarchar](50) NOT NULL,
	[AccountName] [nvarchar](100) NOT NULL,
	[Currency] [nvarchar](3) NOT NULL,
	[Balance] [decimal](18, 5) NOT NULL,
	[Margin] [decimal](18, 5) NOT NULL,
	[Profit] [decimal](18, 5) NOT NULL,
 CONSTRAINT [PK_SimulatedMarginAccounts] PRIMARY KEY CLUSTERED 
(
	[UserName] ASC,
	[AccountName] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]
GO

/****** Object:  Table [dbo].[SimulatedMarginPositions]    Script Date: 2017-07-02 21:46:14 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[SimulatedMarginPositions](
	[Symbol] [nvarchar](24) NOT NULL,
	[UserName] [nvarchar](50) NOT NULL,
	[Account] [nvarchar](100) NOT NULL,
	[BrokerName] [nvarchar](32) NOT NULL,
	[Created] [datetime2](7) NOT NULL,
	[Quantity] [decimal](18, 5) NOT NULL,
	[Price] [decimal](18, 5) NOT NULL,
	[Profit] [decimal](18, 5) NOT NULL,
 CONSTRAINT [PK_SimulatedMarginPositions] PRIMARY KEY CLUSTERED 
(
	[Symbol] ASC,
	[UserName] ASC,
	[Account] ASC,
	[BrokerName] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[SimulatedSymbols]    Script Date: 8/7/2018 4:45:08 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[SimulatedSymbols](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[Symbol] [nvarchar](24) NOT NULL,
	[StartPrice] [decimal](18, 5) NOT NULL,
    	[Currency] [nvarchar](24) NOT NULL,
    	[Margin] [decimal](18, 5) NOT NULL,
    	[CommissionType] [int] NOT NULL,
    	[CommissionValue] [decimal](18, 5) NOT NULL,
    	[ContractSize] [decimal](18, 5) NOT NULL,  
 CONSTRAINT [PK_SimulatedSymbols] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[UserActivity]    Script Date: 2017-07-02 21:46:14 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[UserActivity](
	[ID] [int] IDENTITY(1,1) NOT NULL,
	[User] [nvarchar](50) NOT NULL,
	[Broker] [nvarchar](32) NOT NULL,
	[Account] [nvarchar](50) NOT NULL,
	[Date] [datetime] NOT NULL,
PRIMARY KEY CLUSTERED 
(
	[ID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]
GO
CREATE TABLE [dbo].[Signals](
	[ID] [int] IDENTITY(1,1) NOT NULL,
	[SignalID] [nvarchar](50) NOT NULL,
	[UserLogin] [nvarchar](50) NOT NULL,
	[SignalName] [nvarchar](50) NOT NULL,
	[Date] [datetime] NOT NULL,
	[DataBaseEntry] [datetime] DEFAULT GETUTCDATE() NOT NULL,
PRIMARY KEY CLUSTERED 
(
	[ID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[Users]    Script Date: 2017-07-02 21:46:14 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Users](
	[ID] [int] IDENTITY(1,1) NOT NULL,
	[Login] [nvarchar](50) NOT NULL,
	[Password] [nvarchar](50) NOT NULL,
	[Active] [bit] NOT NULL,
 CONSTRAINT [PK_UserId] PRIMARY KEY CLUSTERED 
(
	[Login] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY],
UNIQUE NONCLUSTERED 
(
	[ID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY],
UNIQUE NONCLUSTERED 
(
	[Login] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]
GO
ALTER TABLE [dbo].[BarsDaily] ADD  DEFAULT ('') FOR [Symbol]
GO
ALTER TABLE [dbo].[BarsDaily] ADD  DEFAULT ('') FOR [DataFeed]
GO
ALTER TABLE [dbo].[BarsMinute] ADD  DEFAULT ('') FOR [Symbol]
GO
ALTER TABLE [dbo].[BarsMinute] ADD  DEFAULT ('') FOR [DataFeed]
GO
ALTER TABLE [dbo].[BarsMinute_Archive] ADD  DEFAULT ('') FOR [Symbol]
GO
ALTER TABLE [dbo].[BarsMinute_Archive] ADD  DEFAULT ('') FOR [DataFeed]
GO
ALTER TABLE [dbo].[Orders] ADD  CONSTRAINT [DF_PositionHistory_OpeningQuantity]  DEFAULT ((0)) FOR [OpeningQuantity]
GO
ALTER TABLE [dbo].[Orders] ADD  CONSTRAINT [DF_PositionHistory_ClosingQuantity]  DEFAULT ((0)) FOR [ClosingQuantity]
GO
ALTER TABLE [dbo].[Orders] ADD  DEFAULT ('') FOR [Symbol]
GO
ALTER TABLE [dbo].[Orders] ADD  DEFAULT ('') FOR [DataFeed]
GO
ALTER TABLE [dbo].[PortfolioStrategies] ADD  CONSTRAINT [DF__Portfolio__Expos__3A4CA8FD]  DEFAULT ((0)) FOR [ExposedBalance]
GO
ALTER TABLE [dbo].[PortfolioAccounts]  WITH CHECK ADD  CONSTRAINT [FK_Accounts] FOREIGN KEY([Portfolio_ID])
REFERENCES [dbo].[Portfolios] ([ID])
ON UPDATE CASCADE
ON DELETE CASCADE
GO
ALTER TABLE [dbo].[PortfolioAccounts] CHECK CONSTRAINT [FK_Accounts]
GO
ALTER TABLE [dbo].[Portfolios]  WITH CHECK ADD  CONSTRAINT [FK_Portfolios] FOREIGN KEY([User])
REFERENCES [dbo].[Users] ([Login])
ON UPDATE CASCADE
ON DELETE CASCADE
GO
ALTER TABLE [dbo].[Portfolios] CHECK CONSTRAINT [FK_Portfolios]
GO
ALTER TABLE [dbo].[PortfolioStrategies]  WITH CHECK ADD  CONSTRAINT [FK_Strategies] FOREIGN KEY([Portfolio_ID])
REFERENCES [dbo].[Portfolios] ([ID])
ON UPDATE CASCADE
ON DELETE CASCADE
GO
ALTER TABLE [dbo].[PortfolioStrategies] CHECK CONSTRAINT [FK_Strategies]
GO
ALTER TABLE [dbo].[UserActivity]  WITH CHECK ADD  CONSTRAINT [FK_UserActivity] FOREIGN KEY([User])
REFERENCES [dbo].[Users] ([Login])
GO
ALTER TABLE [dbo].[UserActivity] CHECK CONSTRAINT [FK_UserActivity]
GO
ALTER TABLE [dbo].[Orders]  WITH CHECK ADD CHECK  (([Status]='CANCELED' OR [Status]='FILLED'))
GO
ALTER TABLE [dbo].[Orders]  WITH CHECK ADD CHECK  (([Type]='STOP' OR [Type]='LIMIT' OR [Type]='MARKET'))
GO
ALTER TABLE [dbo].[Orders]  WITH CHECK ADD CHECK  (([TIF]='GTC' OR [TIF]='GFD' OR [TIF]='IOC' OR [TIF]='FOK'))
GO
ALTER TABLE [dbo].[Portfolios]  WITH CHECK ADD CHECK  (([Currency]='GBP' OR [Currency]='USD' OR [Currency]='EUR'))
GO