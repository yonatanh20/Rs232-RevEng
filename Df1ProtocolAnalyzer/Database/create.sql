DROP TABLE [Controls];
CREATE TABLE [Controls] (
  [Id] text NOT NULL
, [Name] text NOT NULL
, [FileNumber] int NOT NULL
, [Element] int NOT NULL
, [SubElement] int NOT NULL
, [FileType] text NOT NULL
, [Offset] int NOT NULL
, CONSTRAINT [sqlite_autoindex_Controls_1] PRIMARY KEY ([Id])
);
CREATE UNIQUE INDEX [index_name] on [Controls]([Name]);