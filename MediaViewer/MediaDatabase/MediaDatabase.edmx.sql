
-- --------------------------------------------------
-- Entity Designer DDL Script for SQL Server 2005, 2008, and Azure
-- --------------------------------------------------
-- Date Created: 01/17/2014 22:03:28
-- Generated from EDMX file: D:\Repos\mediaviewer\MediaViewer\MediaDatabase\MediaDatabase.edmx
-- --------------------------------------------------

SET QUOTED_IDENTIFIER OFF;
GO
USE [MediaViewer.MediaDatabase.MediaDatabaseContext];
GO
IF SCHEMA_ID(N'dbo') IS NULL EXECUTE(N'CREATE SCHEMA [dbo]');
GO

-- --------------------------------------------------
-- Dropping existing FOREIGN KEY constraints
-- --------------------------------------------------

IF OBJECT_ID(N'[dbo].[FK_TagCategoryTag]', 'F') IS NOT NULL
    ALTER TABLE [dbo].[TagSet] DROP CONSTRAINT [FK_TagCategoryTag];
GO
IF OBJECT_ID(N'[dbo].[FK_MediaTag_Media]', 'F') IS NOT NULL
    ALTER TABLE [dbo].[MediaTag] DROP CONSTRAINT [FK_MediaTag_Media];
GO
IF OBJECT_ID(N'[dbo].[FK_MediaTag_Tag]', 'F') IS NOT NULL
    ALTER TABLE [dbo].[MediaTag] DROP CONSTRAINT [FK_MediaTag_Tag];
GO
IF OBJECT_ID(N'[dbo].[FK_PresetMetadataTag_PresetMetadata]', 'F') IS NOT NULL
    ALTER TABLE [dbo].[PresetMetadataTag] DROP CONSTRAINT [FK_PresetMetadataTag_PresetMetadata];
GO
IF OBJECT_ID(N'[dbo].[FK_PresetMetadataTag_Tag]', 'F') IS NOT NULL
    ALTER TABLE [dbo].[PresetMetadataTag] DROP CONSTRAINT [FK_PresetMetadataTag_Tag];
GO
IF OBJECT_ID(N'[dbo].[FK_TagTag_Tag]', 'F') IS NOT NULL
    ALTER TABLE [dbo].[TagTag] DROP CONSTRAINT [FK_TagTag_Tag];
GO
IF OBJECT_ID(N'[dbo].[FK_TagTag_Tag1]', 'F') IS NOT NULL
    ALTER TABLE [dbo].[TagTag] DROP CONSTRAINT [FK_TagTag_Tag1];
GO
IF OBJECT_ID(N'[dbo].[FK_MediaThumbnail]', 'F') IS NOT NULL
    ALTER TABLE [dbo].[ThumbnailSet] DROP CONSTRAINT [FK_MediaThumbnail];
GO

-- --------------------------------------------------
-- Dropping existing tables
-- --------------------------------------------------

IF OBJECT_ID(N'[dbo].[TagSet]', 'U') IS NOT NULL
    DROP TABLE [dbo].[TagSet];
GO
IF OBJECT_ID(N'[dbo].[TagCategorySet]', 'U') IS NOT NULL
    DROP TABLE [dbo].[TagCategorySet];
GO
IF OBJECT_ID(N'[dbo].[MediaSet]', 'U') IS NOT NULL
    DROP TABLE [dbo].[MediaSet];
GO
IF OBJECT_ID(N'[dbo].[PresetMetadataSet]', 'U') IS NOT NULL
    DROP TABLE [dbo].[PresetMetadataSet];
GO
IF OBJECT_ID(N'[dbo].[ThumbnailSet]', 'U') IS NOT NULL
    DROP TABLE [dbo].[ThumbnailSet];
GO
IF OBJECT_ID(N'[dbo].[MediaTag]', 'U') IS NOT NULL
    DROP TABLE [dbo].[MediaTag];
GO
IF OBJECT_ID(N'[dbo].[PresetMetadataTag]', 'U') IS NOT NULL
    DROP TABLE [dbo].[PresetMetadataTag];
GO
IF OBJECT_ID(N'[dbo].[TagTag]', 'U') IS NOT NULL
    DROP TABLE [dbo].[TagTag];
GO

-- --------------------------------------------------
-- Creating all tables
-- --------------------------------------------------

-- Creating table 'TagSet'
CREATE TABLE [dbo].[TagSet] (
    [Id] int IDENTITY(1,1) NOT NULL,
    [Name] nvarchar(max)  NOT NULL,
    [TagCategoryId] int  NULL
);
GO

-- Creating table 'TagCategorySet'
CREATE TABLE [dbo].[TagCategorySet] (
    [Id] int IDENTITY(1,1) NOT NULL,
    [Name] nvarchar(max)  NOT NULL
);
GO

-- Creating table 'MediaSet'
CREATE TABLE [dbo].[MediaSet] (
    [Id] int IDENTITY(1,1) NOT NULL,
    [Location] nvarchar(max)  NOT NULL,
    [Title] nvarchar(max)  NULL,
    [Rating] float  NULL,
    [Description] nvarchar(max)  NULL,
    [Author] nvarchar(max)  NULL,
    [Copyright] nvarchar(max)  NULL,
    [LastModified] datetime  NOT NULL,
    [CreationDate] datetime  NULL,
    [MimeType] nvarchar(max)  NOT NULL,
    [SizeBytes] bigint  NOT NULL,
    [VideoProps_VideoContainer] nvarchar(max)  NULL,
    [VideoProps_VideoCodec] nvarchar(max)  NULL,
    [VideoProps_Width] bigint  NULL,
    [VideoProps_Height] bigint  NULL,
    [VideoProps_DurationSeconds] bigint  NULL,
    [VideoProps_PixelFormat] nvarchar(max)  NULL,
    [VideoProps_FramesPerSecond] float  NULL,
    [VideoProps_SamplesPerSecond] smallint  NULL,
    [VideoProps_NrChannels] smallint  NULL,
    [VideoProps_BitsPerSample] smallint  NULL,
    [ImageProps_Width] int  NULL,
    [ImageProps_Height] int  NULL
);
GO

-- Creating table 'PresetMetadataSet'
CREATE TABLE [dbo].[PresetMetadataSet] (
    [Id] int IDENTITY(1,1) NOT NULL,
    [Name] nvarchar(max)  NOT NULL,
    [Title] nvarchar(max)  NULL,
    [Rating] float  NULL,
    [Description] nvarchar(max)  NULL,
    [Author] nvarchar(max)  NULL,
    [Copyright] nvarchar(max)  NULL,
    [IsNameEnabled] bit  NOT NULL,
    [IsTitleEnabled] bit  NOT NULL,
    [IsRatingEnabled] bit  NOT NULL,
    [IsDescriptionEnabled] bit  NOT NULL,
    [IsAuthorEnabled] bit  NOT NULL,
    [IsCopyrightEnabled] bit  NOT NULL,
    [CreationDate] datetime  NOT NULL,
    [IsCreationDateEnabled] bit  NOT NULL
);
GO

-- Creating table 'ThumbnailSet'
CREATE TABLE [dbo].[ThumbnailSet] (
    [Id] int IDENTITY(1,1) NOT NULL,
    [ImageData] varbinary(max)  NOT NULL,
    [Width] smallint  NOT NULL,
    [Height] smallint  NOT NULL,
    [Media_Id] int  NOT NULL
);
GO

-- Creating table 'MediaTag'
CREATE TABLE [dbo].[MediaTag] (
    [Media_Id] int  NOT NULL,
    [Tags_Id] int  NOT NULL
);
GO

-- Creating table 'PresetMetadataTag'
CREATE TABLE [dbo].[PresetMetadataTag] (
    [PresetMetadataTag_Tag_Id] int  NOT NULL,
    [Tags_Id] int  NOT NULL
);
GO

-- Creating table 'TagTag'
CREATE TABLE [dbo].[TagTag] (
    [ParentTags_Id] int  NOT NULL,
    [ChildTags_Id] int  NOT NULL
);
GO

-- --------------------------------------------------
-- Creating all PRIMARY KEY constraints
-- --------------------------------------------------

-- Creating primary key on [Id] in table 'TagSet'
ALTER TABLE [dbo].[TagSet]
ADD CONSTRAINT [PK_TagSet]
    PRIMARY KEY CLUSTERED ([Id] ASC);
GO

-- Creating primary key on [Id] in table 'TagCategorySet'
ALTER TABLE [dbo].[TagCategorySet]
ADD CONSTRAINT [PK_TagCategorySet]
    PRIMARY KEY CLUSTERED ([Id] ASC);
GO

-- Creating primary key on [Id] in table 'MediaSet'
ALTER TABLE [dbo].[MediaSet]
ADD CONSTRAINT [PK_MediaSet]
    PRIMARY KEY CLUSTERED ([Id] ASC);
GO

-- Creating primary key on [Id] in table 'PresetMetadataSet'
ALTER TABLE [dbo].[PresetMetadataSet]
ADD CONSTRAINT [PK_PresetMetadataSet]
    PRIMARY KEY CLUSTERED ([Id] ASC);
GO

-- Creating primary key on [Id] in table 'ThumbnailSet'
ALTER TABLE [dbo].[ThumbnailSet]
ADD CONSTRAINT [PK_ThumbnailSet]
    PRIMARY KEY CLUSTERED ([Id] ASC);
GO

-- Creating primary key on [Media_Id], [Tags_Id] in table 'MediaTag'
ALTER TABLE [dbo].[MediaTag]
ADD CONSTRAINT [PK_MediaTag]
    PRIMARY KEY NONCLUSTERED ([Media_Id], [Tags_Id] ASC);
GO

-- Creating primary key on [PresetMetadataTag_Tag_Id], [Tags_Id] in table 'PresetMetadataTag'
ALTER TABLE [dbo].[PresetMetadataTag]
ADD CONSTRAINT [PK_PresetMetadataTag]
    PRIMARY KEY NONCLUSTERED ([PresetMetadataTag_Tag_Id], [Tags_Id] ASC);
GO

-- Creating primary key on [ParentTags_Id], [ChildTags_Id] in table 'TagTag'
ALTER TABLE [dbo].[TagTag]
ADD CONSTRAINT [PK_TagTag]
    PRIMARY KEY NONCLUSTERED ([ParentTags_Id], [ChildTags_Id] ASC);
GO

-- --------------------------------------------------
-- Creating all FOREIGN KEY constraints
-- --------------------------------------------------

-- Creating foreign key on [TagCategoryId] in table 'TagSet'
ALTER TABLE [dbo].[TagSet]
ADD CONSTRAINT [FK_TagCategoryTag]
    FOREIGN KEY ([TagCategoryId])
    REFERENCES [dbo].[TagCategorySet]
        ([Id])
    ON DELETE NO ACTION ON UPDATE NO ACTION;

-- Creating non-clustered index for FOREIGN KEY 'FK_TagCategoryTag'
CREATE INDEX [IX_FK_TagCategoryTag]
ON [dbo].[TagSet]
    ([TagCategoryId]);
GO

-- Creating foreign key on [Media_Id] in table 'MediaTag'
ALTER TABLE [dbo].[MediaTag]
ADD CONSTRAINT [FK_MediaTag_Media]
    FOREIGN KEY ([Media_Id])
    REFERENCES [dbo].[MediaSet]
        ([Id])
    ON DELETE NO ACTION ON UPDATE NO ACTION;
GO

-- Creating foreign key on [Tags_Id] in table 'MediaTag'
ALTER TABLE [dbo].[MediaTag]
ADD CONSTRAINT [FK_MediaTag_Tag]
    FOREIGN KEY ([Tags_Id])
    REFERENCES [dbo].[TagSet]
        ([Id])
    ON DELETE NO ACTION ON UPDATE NO ACTION;

-- Creating non-clustered index for FOREIGN KEY 'FK_MediaTag_Tag'
CREATE INDEX [IX_FK_MediaTag_Tag]
ON [dbo].[MediaTag]
    ([Tags_Id]);
GO

-- Creating foreign key on [PresetMetadataTag_Tag_Id] in table 'PresetMetadataTag'
ALTER TABLE [dbo].[PresetMetadataTag]
ADD CONSTRAINT [FK_PresetMetadataTag_PresetMetadata]
    FOREIGN KEY ([PresetMetadataTag_Tag_Id])
    REFERENCES [dbo].[PresetMetadataSet]
        ([Id])
    ON DELETE NO ACTION ON UPDATE NO ACTION;
GO

-- Creating foreign key on [Tags_Id] in table 'PresetMetadataTag'
ALTER TABLE [dbo].[PresetMetadataTag]
ADD CONSTRAINT [FK_PresetMetadataTag_Tag]
    FOREIGN KEY ([Tags_Id])
    REFERENCES [dbo].[TagSet]
        ([Id])
    ON DELETE NO ACTION ON UPDATE NO ACTION;

-- Creating non-clustered index for FOREIGN KEY 'FK_PresetMetadataTag_Tag'
CREATE INDEX [IX_FK_PresetMetadataTag_Tag]
ON [dbo].[PresetMetadataTag]
    ([Tags_Id]);
GO

-- Creating foreign key on [ParentTags_Id] in table 'TagTag'
ALTER TABLE [dbo].[TagTag]
ADD CONSTRAINT [FK_TagTag_Tag]
    FOREIGN KEY ([ParentTags_Id])
    REFERENCES [dbo].[TagSet]
        ([Id])
    ON DELETE NO ACTION ON UPDATE NO ACTION;
GO

-- Creating foreign key on [ChildTags_Id] in table 'TagTag'
ALTER TABLE [dbo].[TagTag]
ADD CONSTRAINT [FK_TagTag_Tag1]
    FOREIGN KEY ([ChildTags_Id])
    REFERENCES [dbo].[TagSet]
        ([Id])
    ON DELETE NO ACTION ON UPDATE NO ACTION;

-- Creating non-clustered index for FOREIGN KEY 'FK_TagTag_Tag1'
CREATE INDEX [IX_FK_TagTag_Tag1]
ON [dbo].[TagTag]
    ([ChildTags_Id]);
GO

-- Creating foreign key on [Media_Id] in table 'ThumbnailSet'
ALTER TABLE [dbo].[ThumbnailSet]
ADD CONSTRAINT [FK_MediaThumbnail]
    FOREIGN KEY ([Media_Id])
    REFERENCES [dbo].[MediaSet]
        ([Id])
    ON DELETE NO ACTION ON UPDATE NO ACTION;

-- Creating non-clustered index for FOREIGN KEY 'FK_MediaThumbnail'
CREATE INDEX [IX_FK_MediaThumbnail]
ON [dbo].[ThumbnailSet]
    ([Media_Id]);
GO

-- --------------------------------------------------
-- Script has ended
-- --------------------------------------------------