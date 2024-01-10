namespace Lab2;

using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;

public class CatalogContext : DbContext
{
    public DbSet<Record> Compositions { get; set; }

    public string DbPath { get; }

    public CatalogContext(string path)
    {
        DbPath = path;
        Database.EnsureCreated();
    }

    protected override void OnConfiguring(DbContextOptionsBuilder options)
        => options.UseSqlite($"Data Source={DbPath}");
}

public class Record
{
    public int RecordId { get; set; }
    public string Author { get; set; }
    public string Composition { get; set; }
}