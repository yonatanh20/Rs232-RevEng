-- Script Date: 9/23/2017 10:19 PM  - ErikEJ.SqlCeScripting version 3.5.2.72
DROP TABLE [History];
CREATE TABLE [History] (
  [Id] text NOT NULL
, [Name] text NOT NULL
, [TimeStamp] text NOT NULL
, [NewState] bigint NOT NULL
, CONSTRAINT [sqlite_autoindex_Change History_1] PRIMARY KEY ([Id] , [Timestamp])
);
