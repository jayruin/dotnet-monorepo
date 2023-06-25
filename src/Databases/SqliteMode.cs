namespace Databases;

// https://www.sqlite.org/c3ref/open.html
public enum SqliteOpenMode
{
    ReadOnly,
    ReadWrite,
    ReadWriteCreate,
    Memory,
}
