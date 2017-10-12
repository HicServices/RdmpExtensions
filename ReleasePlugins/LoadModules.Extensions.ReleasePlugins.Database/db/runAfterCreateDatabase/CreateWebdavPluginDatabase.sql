/****** Object:  Table [dbo].[AutomateExtraction]    Script Date: 05/07/2017 08:37:46 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[WebdavAutomationAudit](
	[ID] [int] IDENTITY(1,1) NOT NULL,
	[FileHref] [varchar(4000)] NOT NULL,
	[FileResult] [int] NOT NULL,
	[Message] [varchar(4000)] NOT NULL,
 CONSTRAINT [PK_WebdavAutomationAudit] PRIMARY KEY CLUSTERED 
(
	[ID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]

GO
ALTER TABLE [dbo].[WebdavAutomationAudit] ADD  CONSTRAINT [DF_WebdavAutomationAudit_FileResult]  DEFAULT ((0)) FOR [FileResult]
GO
