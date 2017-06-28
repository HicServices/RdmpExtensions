/****** Object:  Table [dbo].[AutomateExtraction]    Script Date: 28/06/2017 14:44:49 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[AutomateExtraction](
	[ID] [int] IDENTITY(1,1) NOT NULL,
	[ExtractionConfiguration_ID] [int] NOT NULL,
	[LastAttempt] [datetime] NULL,
	[LastAttemptDataLoadRunID] [int] NULL,
	[Pipeline_ID] [int] NULL,
	[ExecutionSchedule_ID] [int] NOT NULL,
	[SuccessfullyExtractedResults_ID] [int] NULL,
 CONSTRAINT [PK_AutomateExtraction] PRIMARY KEY CLUSTERED 
(
	[ID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]

GO
/****** Object:  Table [dbo].[ExecutionSchedule]    Script Date: 28/06/2017 14:44:49 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[ExecutionSchedule](
	[ID] [int] IDENTITY(1,1) NOT NULL,
	[ExecutionTimescale] [varchar](50) NOT NULL,
	[UserRequestingRefresh] [varchar](500) NULL,
	[UserRequestingRefreshDate] [datetime] NULL,
	[Ticket] [varchar](500) NULL,
	[Name] [varchar](500) NOT NULL,
	[Comment] [varchar](max) NULL,
	[Disabled] [bit] NOT NULL,
	[Project_ID] [int] NOT NULL,
 CONSTRAINT [PK_ExecutionSchedule] PRIMARY KEY CLUSTERED 
(
	[ID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]

GO
/****** Object:  Table [dbo].[ReleaseIdentifierExtracted]    Script Date: 28/06/2017 14:44:49 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[ReleaseIdentifierExtracted](
	[SuccessfullyExtractedResults_ID] [int] NOT NULL,
	[ReleaseIdentifier] [varchar](500) NOT NULL,
 CONSTRAINT [PK_ReleaseIdentifierExtracted] PRIMARY KEY CLUSTERED 
(
	[SuccessfullyExtractedResults_ID] ASC,
	[ReleaseIdentifier] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]

GO
/****** Object:  Table [dbo].[SuccessfullyExtractedResults]    Script Date: 28/06/2017 14:44:49 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[SuccessfullyExtractedResults](
	[ID] [int] IDENTITY(1,1) NOT NULL,
	[SQL] [varchar](max) NOT NULL,
	[ExtractDate] [datetime] NOT NULL,
 CONSTRAINT [PK_SuccessfullyExtractedResults] PRIMARY KEY CLUSTERED 
(
	[ID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]

GO
ALTER TABLE [dbo].[ExecutionSchedule] ADD  CONSTRAINT [DF_ExecutionSchedule_Disabled]  DEFAULT ((0)) FOR [Disabled]
GO
ALTER TABLE [dbo].[SuccessfullyExtractedResults] ADD  CONSTRAINT [DF_SuccessfullyExtractedResults_ExtractDate]  DEFAULT (getdate()) FOR [ExtractDate]
GO
ALTER TABLE [dbo].[AutomateExtraction]  WITH CHECK ADD  CONSTRAINT [FK_AutomateExtraction_ExecutionSchedule] FOREIGN KEY([ExecutionSchedule_ID])
REFERENCES [dbo].[ExecutionSchedule] ([ID])
GO
ALTER TABLE [dbo].[AutomateExtraction] CHECK CONSTRAINT [FK_AutomateExtraction_ExecutionSchedule]
GO
ALTER TABLE [dbo].[AutomateExtraction]  WITH CHECK ADD  CONSTRAINT [FK_AutomateExtraction_SuccessfullyExtractedResults] FOREIGN KEY([SuccessfullyExtractedResults_ID])
REFERENCES [dbo].[SuccessfullyExtractedResults] ([ID])
GO
ALTER TABLE [dbo].[AutomateExtraction] CHECK CONSTRAINT [FK_AutomateExtraction_SuccessfullyExtractedResults]
GO
ALTER TABLE [dbo].[ReleaseIdentifierExtracted]  WITH CHECK ADD  CONSTRAINT [FK_ReleaseIdentifierExtracted_SuccessfullyExtractedResults] FOREIGN KEY([SuccessfullyExtractedResults_ID])
REFERENCES [dbo].[SuccessfullyExtractedResults] ([ID])
GO
ALTER TABLE [dbo].[ReleaseIdentifierExtracted] CHECK CONSTRAINT [FK_ReleaseIdentifierExtracted_SuccessfullyExtractedResults]
GO
