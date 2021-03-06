USE [AGVC_CAAGV100_NEW]
GO
DROP TABLE [dbo].[UASFNC]
DROP TABLE [dbo].[UASUFNC]
DROP TABLE [dbo].[UASUSR]
DROP TABLE [dbo].[UASUSRGRP]
/****** Object:  Table [dbo].[UASFNC]    Script Date: 2019/5/31 下午 04:59:23 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[UASFNC](
	[FUNC_CODE] [char](60) NOT NULL,
	[FUNC_NAME] [char](80) NULL,
 CONSTRAINT [PK_UASFNC] PRIMARY KEY CLUSTERED 
(
	[FUNC_CODE] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[UASUFNC]    Script Date: 2019/5/31 下午 04:59:23 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[UASUFNC](
	[USER_GRP] [char](20) NOT NULL,
	[FUNC_CODE] [char](60) NOT NULL,
 CONSTRAINT [PK_UASUFNC] PRIMARY KEY CLUSTERED 
(
	[USER_GRP] ASC,
	[FUNC_CODE] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[UASUSR]    Script Date: 2019/5/31 下午 04:59:23 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[UASUSR](
	[USER_ID] [char](20) NOT NULL,
	[PASSWD] [varchar](max) NULL,
	[BADGE_NUMBER] [char](80) NULL,
	[USER_NAME] [char](30) NULL,
	[DISABLE_FLG] [char](1) NULL,
	[POWER_USER_FLG] [char](1) NULL,
	[ADMIN_FLG] [char](1) NULL,
	[USER_GRP] [char](20) NULL,
	[DEPARTMENT] [char](20) NULL,
 CONSTRAINT [PK_UASUSR] PRIMARY KEY CLUSTERED 
(
	[USER_ID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO
/****** Object:  Table [dbo].[UASUSRGRP]    Script Date: 2019/5/31 下午 04:59:23 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[UASUSRGRP](
	[USER_GRP] [char](20) NOT NULL,
 CONSTRAINT [PK_UASUSRGRP] PRIMARY KEY CLUSTERED 
(
	[USER_GRP] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]
GO
INSERT [dbo].[UASFNC] ([FUNC_CODE], [FUNC_NAME]) VALUES (N'FUNC_ACCOUNT_MANAGEMENT                                     ', N'User Account Management                                                         ')
INSERT [dbo].[UASFNC] ([FUNC_CODE], [FUNC_NAME]) VALUES (N'FUNC_ADVANCED_SETTING                                       ', N'Advanced Setting                                                                ')
INSERT [dbo].[UASFNC] ([FUNC_CODE], [FUNC_NAME]) VALUES (N'FUNC_ADVANCED_SETTINGS                                      ', N'Advanced Settings                                                               ')
INSERT [dbo].[UASFNC] ([FUNC_CODE], [FUNC_NAME]) VALUES (N'FUNC_CLOSE_SYSTEM                                           ', N'System Shutdown                                                                 ')
INSERT [dbo].[UASFNC] ([FUNC_CODE], [FUNC_NAME]) VALUES (N'FUNC_DEBUG                                                  ', N'Debug                                                                           ')
INSERT [dbo].[UASFNC] ([FUNC_CODE], [FUNC_NAME]) VALUES (N'FUNC_LOGIN                                                  ', N'User Login                                                                      ')
INSERT [dbo].[UASFNC] ([FUNC_CODE], [FUNC_NAME]) VALUES (N'FUNC_PORT_MAINTENANCE                                       ', N'Port Maintenance                                                                ')
INSERT [dbo].[UASFNC] ([FUNC_CODE], [FUNC_NAME]) VALUES (N'FUNC_SYSTEM_CONCROL_MODE                                    ', N'System Control                                                                  ')
INSERT [dbo].[UASFNC] ([FUNC_CODE], [FUNC_NAME]) VALUES (N'FUNC_TRANSFER_MANAGEMENT                                    ', N'Transfer Management                                                             ')
INSERT [dbo].[UASFNC] ([FUNC_CODE], [FUNC_NAME]) VALUES (N'FUNC_VEHICLE_MANAGEMENT                                     ', N'Vehicle Management                                                              ')
INSERT [dbo].[UASUFNC] ([USER_GRP], [FUNC_CODE]) VALUES (N'ADMIN               ', N'FUNC_ACCOUNT_MANAGEMENT                                     ')
INSERT [dbo].[UASUFNC] ([USER_GRP], [FUNC_CODE]) VALUES (N'ADMIN               ', N'FUNC_ADVANCED_SETTING                                       ')
INSERT [dbo].[UASUFNC] ([USER_GRP], [FUNC_CODE]) VALUES (N'ADMIN               ', N'FUNC_CLOSE_SYSTEM                                           ')
INSERT [dbo].[UASUFNC] ([USER_GRP], [FUNC_CODE]) VALUES (N'ADMIN               ', N'FUNC_DEBUG                                                  ')
INSERT [dbo].[UASUFNC] ([USER_GRP], [FUNC_CODE]) VALUES (N'ADMIN               ', N'FUNC_LOGIN                                                  ')
INSERT [dbo].[UASUFNC] ([USER_GRP], [FUNC_CODE]) VALUES (N'ADMIN               ', N'FUNC_PORT_MAINTENANCE                                       ')
INSERT [dbo].[UASUFNC] ([USER_GRP], [FUNC_CODE]) VALUES (N'ADMIN               ', N'FUNC_SYSTEM_CONCROL_MODE                                    ')
INSERT [dbo].[UASUFNC] ([USER_GRP], [FUNC_CODE]) VALUES (N'ADMIN               ', N'FUNC_TRANSFER_MANAGEMENT                                    ')
INSERT [dbo].[UASUFNC] ([USER_GRP], [FUNC_CODE]) VALUES (N'ADMIN               ', N'FUNC_VEHICLE_MANAGEMENT                                     ')
INSERT [dbo].[UASUFNC] ([USER_GRP], [FUNC_CODE]) VALUES (N'ENG                 ', N'FUNC_ACCOUNT_MANAGEMENT                                     ')
INSERT [dbo].[UASUFNC] ([USER_GRP], [FUNC_CODE]) VALUES (N'ENG                 ', N'FUNC_ADVANCED_SETTING                                       ')
INSERT [dbo].[UASUFNC] ([USER_GRP], [FUNC_CODE]) VALUES (N'ENG                 ', N'FUNC_CLOSE_SYSTEM                                           ')
INSERT [dbo].[UASUFNC] ([USER_GRP], [FUNC_CODE]) VALUES (N'ENG                 ', N'FUNC_DEBUG                                                  ')
INSERT [dbo].[UASUFNC] ([USER_GRP], [FUNC_CODE]) VALUES (N'ENG                 ', N'FUNC_LOGIN                                                  ')
INSERT [dbo].[UASUFNC] ([USER_GRP], [FUNC_CODE]) VALUES (N'ENG                 ', N'FUNC_PORT_MAINTENANCE                                       ')
INSERT [dbo].[UASUFNC] ([USER_GRP], [FUNC_CODE]) VALUES (N'ENG                 ', N'FUNC_SYSTEM_CONCROL_MODE                                    ')
INSERT [dbo].[UASUFNC] ([USER_GRP], [FUNC_CODE]) VALUES (N'ENG                 ', N'FUNC_TRANSFER_MANAGEMENT                                    ')
INSERT [dbo].[UASUFNC] ([USER_GRP], [FUNC_CODE]) VALUES (N'ENG                 ', N'FUNC_VEHICLE_MANAGEMENT                                     ')
INSERT [dbo].[UASUSR] ([USER_ID], [PASSWD], [BADGE_NUMBER], [USER_NAME], [DISABLE_FLG], [POWER_USER_FLG], [ADMIN_FLG], [USER_GRP], [DEPARTMENT]) VALUES (N'ADMIN               ', N'ilovecsot', N'123456                                                                          ', N'ADMI                          ', N'N', N'N', N'N', N'ADMIN               ', N'3K3                 ')
INSERT [dbo].[UASUSR] ([USER_ID], [PASSWD], [BADGE_NUMBER], [USER_NAME], [DISABLE_FLG], [POWER_USER_FLG], [ADMIN_FLG], [USER_GRP], [DEPARTMENT]) VALUES (N'MARK                ', N'a0369036', N'123456                                                                          ', N'ADMI                          ', N'N', N'N', N'N', N'ENG                 ', N'3K3                 ')
INSERT [dbo].[UASUSRGRP] ([USER_GRP]) VALUES (N'ADMIN               ')
INSERT [dbo].[UASUSRGRP] ([USER_GRP]) VALUES (N'ENG                 ')
INSERT [dbo].[UASUSRGRP] ([USER_GRP]) VALUES (N'OP                  ')
