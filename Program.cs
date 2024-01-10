using System;
using System.Dynamic;
using System.Net.NetworkInformation;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices.Marshalling;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Xml.Serialization;
using System.Xml.XPath;
using Microsoft.EntityFrameworkCore;

namespace Lab2 // Note: actual namespace depends on the project name.
{
enum StorageType {
    JSON,
    XML,
    SQLITE
}
public class Author {
    private string name;
    private List<string> compositions = new List<string>();

    private Author() {
        this.name = "";
    }

    public Author(string name) {
        this.name = name;
    }

    public string Name {
        get { return name; }
        set { name = value; }
    }

    public List<string> Compositions {
        get { return compositions; }
        set { compositions = value; }
    }
}
internal class Program
{

    private static List<Author> authors = new List<Author>();
    static void Main(string[] args) {
        // load db from file
        printMan();
        bool exit = false;
        while(!exit) {
            string command = Console.ReadLine();
            switch(command) {
                case "list": 
                    listAll();
                    break;
                case "search": 
                    (string authorPattern, string compositionPattern) = getInputComposition();
                    if (authorPattern == null && compositionPattern == null) {
                        Console.WriteLine("Error! Search pattern is empty");
                    } else {
                        var searchResult = search(authorPattern, compositionPattern);
                        diplayRecords(searchResult);
                    }
                    break;
                case "add": 
                    (string addAuthor, string addComposition) = getInputComposition();
                    if (addAuthor == null || addComposition == null) {
                        Console.WriteLine("Error! Input is empty");
                    } else {
                        add(addAuthor, addComposition);
                    }
                    break;
                case "del":
                    (string delAuthor, string delComposition) = getInputComposition();
                    if (delAuthor == null || delComposition == null) {
                        Console.WriteLine("Error! Input is empty");
                    } else {
                        delete(delAuthor, delComposition);
                    }
                    break;
                case "save":
                    Console.WriteLine("Input filepath to save data(default is MusicLibrary.sqlite):");
                    string outputpath = Console.ReadLine();
                    save(outputpath);
                    break;
                case "load":
                 Console.WriteLine("Input filepath to load data(default is MusicLibrary.sqlite):");
                    string inputPath = Console.ReadLine();
                    load(inputPath);
                    break;
                case "man": 
                        printMan();
                    break;
                case "exit": 
                    exit = true;
                    break;
                default:
                    Console.WriteLine($"{command} is not recognised as registered command!");
                    break;
            }
        } 
    }
    static void printMan() {
        Console.WriteLine(
            "Usage:\n"
            + "    Type one of commands:\n" 
            + "        \"add\" to add new record\n"
            + "        \"del\" to delete record\n"
            + "        \"list\" to display all records of the catalog\n"
            + "        \"load\" to load catalog from file\n"
            + "        \"save\" to save catalog tp JSON/XML/SQlite\n"
            + "        \"search\" to find records in the catalog\n"
            + "        \"man\" to display manual\n"
            + "        \"exit\" to exit\n"
            + "______"
        );
    }

    static void save(string path){
        if(path == "") {
            path =  "MusicLibrary.sqlite";
        }
        string filetype = Path.GetExtension(path);
        switch(filetype) {
            case ".json":
                string json = JsonSerializer.Serialize(authors);
                File.WriteAllText(path, json);
                break;
            case ".xml":
                XmlSerializer catalogSerializer = new XmlSerializer(typeof(List<Author>));
                StreamWriter catalogWriter = new StreamWriter(path);
                catalogSerializer.Serialize(catalogWriter, authors);
                catalogWriter.Close();
                break;
            case ".sqlite":
                // string connectionString = "Data Source=" + path;
                // SqliteConnection connection = new SqliteConnection(connectionString); 
                // connection.Open();
                CatalogContext db = new CatalogContext(path);
                db.Database.ExecuteSqlRaw("DELETE FROM Compositions;");
                foreach(var author in authors) {
                    foreach(var composition in author.Compositions) {
                        
                        db.Add(new Record{Author=author.Name, Composition=composition});
                    }
                }
                db.SaveChanges();
                break;
            default: 
                Console.WriteLine("Error! Incorrect storage type.");
                break;
        }
    }

    static void load(string path){
        if(path == "") {
            path =  "MusicLibrary.sqlite";
        }
        string filetype = Path.GetExtension(path);
        switch(filetype) {
            case ".json":
                string json = File.ReadAllText(path);
                authors = JsonSerializer.Deserialize<List<Author>>(json);
                break;
            case ".xml":
                XmlSerializer catalogSerializer = new XmlSerializer(typeof(List<Author>));
                FileStream catalogStream = new FileStream(path, FileMode.Open);
                authors = (List<Author>)catalogSerializer.Deserialize(catalogStream);
                break;
            case ".sqlite":
                CatalogContext db = new CatalogContext(path);
                foreach(var comp in db.Compositions) {
                    add(comp.Author, comp.Composition);
                }
                break;
            default: 
                Console.WriteLine("Error! Incorrect storage type.");
                break;
        }
    }

    static void diplayRecords(List<(string, string)> records) {
        Console.WriteLine("Found " + records.Count + " records:");
        foreach(var record in records) {
            Console.WriteLine(record.Item1 + " - " + record.Item2);
        }
    } 

    static (string, string) getInputComposition() {
        Console.WriteLine("Input authors name:");
        string author = Console.ReadLine();
        Console.WriteLine("Input composition name:");
        string composition = Console.ReadLine();
        return (author, composition);
    }
    static void listAll() {
        foreach(var author in authors) {
            foreach(var composition in author.Compositions) {
                Console.WriteLine(author.Name + " - " + composition);
            }
        }
    }

    static List<(string, string)> search(string author, string composition) {
        var requiredAuthors = authors; 
        if (author != null) {
            requiredAuthors = requiredAuthors.FindAll(el => el.Name.ToLower().Contains(author.ToLower()));
        }
        List<(string, string)> result = new List<(string, string)>();
        if (composition != null) {
            foreach(var auth in requiredAuthors) {
                var requiredComps = auth.Compositions.FindAll(comp => comp.ToLower().Contains(composition.ToLower()));
                foreach (var comp in requiredComps) {
                    result.Add((auth.Name, comp));
                }
            }
        } else {
            foreach(var auth in requiredAuthors) {
                foreach (var comp in auth.Compositions) {
                    result.Add((auth.Name, comp));
                }
            }
        }
        return result;
    } 

    static void add(string author, string composition) {
        var reqAuthor = authors.Find(el => el.Name.Equals(author));
        if (reqAuthor == null) {
            reqAuthor = new Author(author);
            authors.Add(reqAuthor);
        }
        reqAuthor.Compositions.Add(composition);
        Console.WriteLine(author + " - " + composition + " added to the catalog");
    }

    static void delete(string author, string composition) {
        var reqAuthor = authors.Find(el => el.Name.Equals(author));
        if (reqAuthor != null) {
            bool result = reqAuthor.Compositions.Remove(composition);
            if (result) {
                    Console.WriteLine(author + " - " + composition + " deleted from the catalog");
                    return;
            }
        }
            Console.WriteLine("Error! " + author + " - " + composition + "not found");   
    }
}
}