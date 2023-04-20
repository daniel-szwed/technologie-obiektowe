// See https://aka.ms/new-console-template for more information

using Microsoft.Data.Sqlite;
using TinyOrm;
using TinyOrm.Abstraction.Data;
using TinyOrm.DataProvider;
using TinyOrm.Models;

Console.WriteLine("Hello, World!");
var connectionString = "Data Source=database.db;";

// Create tables if not exist and truncate content
using (var connection = new SqliteConnection(connectionString))
{
    connection.Open();

    var command = connection.CreateCommand();
    command.CommandText =
        @"
        CREATE TABLE IF NOT EXISTS students (
          id INTEGER PRIMARY KEY AUTOINCREMENT,
          firstName TEXT,
          lastName TEXT
        );

        CREATE TABLE IF NOT EXISTS classes (
          id INTEGER PRIMARY KEY AUTOINCREMENT,
          name TEXT
        );

        CREATE TABLE IF NOT EXISTS studentClass (
          student_id INTEGER,
          class_id INTEGER,
          PRIMARY KEY (student_id, class_id),
          FOREIGN KEY (student_id) REFERENCES students(id) ON DELETE CASCADE,
          FOREIGN KEY (class_id) REFERENCES classes(id) ON DELETE CASCADE
        );

        CREATE TABLE IF NOT EXISTS hobbies (
            id INTEGER PRIMARY KEY AUTOINCREMENT,
            name TEXT,
            description TEXT,
            student_id INTEGER,
            FOREIGN KEY (student_id) REFERENCES students(id) ON DELETE CASCADE
        );

        CREATE TABLE IF NOT EXISTS addresses (
            id INTEGER PRIMARY KEY AUTOINCREMENT,
            street TEXT,
            number TEXT,
            zipCode TEXT,
            student_id INTEGER,
            FOREIGN KEY (student_id) REFERENCES students(id) ON DELETE CASCADE
        );

        DELETE FROM hobbies;
        DELETE FROM addresses;
        DELETE FROM studentClass;
        DELETE FROM classes;
        DELETE FROM students;
    ";

    command.ExecuteNonQuery();
}

// Connect to the SQLite database
using (var connection = new SqliteConnection(connectionString))
{
    connection.Open();

    // Insert dummy data into the 'students' table
    using (var command =
           new SqliteCommand("INSERT INTO students (id, firstName, lastName) VALUES (@id, @firstName, @lastName)",
               connection))
    {
        command.Parameters.AddWithValue("@id", 1);
        command.Parameters.AddWithValue("@firstName", "John");
        command.Parameters.AddWithValue("@lastName", "Doe");
        command.ExecuteNonQuery();
    }

    // Insert dummy data into the 'classes' table
    using (var command = new SqliteCommand("INSERT INTO classes (id, name) VALUES (@id, @name)", connection))
    {
        command.Parameters.AddWithValue("@id", 11);
        command.Parameters.AddWithValue("@name", "Math");
        command.ExecuteNonQuery();
    }

    // Insert dummy data into the 'studentClass' table
    using (var command =
           new SqliteCommand("INSERT INTO studentClass (student_id, class_id) VALUES (@student_id, @class_id)",
               connection))
    {
        command.Parameters.AddWithValue("@student_id", 1);
        command.Parameters.AddWithValue("@class_id", 11);
        command.ExecuteNonQuery();
    }

    // Insert dummy data into the 'hobbies' table
    using (var command =
           new SqliteCommand(
               "INSERT INTO hobbies (id, name, description, student_id) VALUES (@id, @name, @description, @student_id)",
               connection))
    {
        command.Parameters.AddWithValue("@id", 1);
        command.Parameters.AddWithValue("@name", "Gardening");
        command.Parameters.AddWithValue("@description", "Planting and nurturing flowers and plants");
        command.Parameters.AddWithValue("@student_id", 1);
        command.ExecuteNonQuery();
    }

    // Insert dummy data into the 'addresses' table
    using (var command =
           new SqliteCommand(
               "INSERT INTO addresses (id, street, number, zipCode, student_id) VALUES (@id, @street, @number, @zipCode, @student_id)",
               connection))
    {
        command.Parameters.AddWithValue("@id", 1);
        command.Parameters.AddWithValue("@street", "123 Main Street");
        command.Parameters.AddWithValue("@number", "5A");
        command.Parameters.AddWithValue("@zipCode", "12345");
        command.Parameters.AddWithValue("@student_id", 1);
        command.ExecuteNonQuery();
    }
}

IDataProvider provider = new SqliteProvider(connectionString);
var student = new Student
{
    FirstName = "Daniel",
    LastName = "Szwed",
    Address = new Address
    {
        Number = "333",
        Street = "Aleje Tysiaclecia",
        ZipCode = "26-110"
    },
    Hobbies = new List<Hobby>
    {
        new()
        {
            Name = "Computer sience",
            Description = "Coding stuff"
        }
    },
    Classes = new List<Class>
    {
        new()
        {
            Name = "Klasa jakas tam"
        }
    }
};
var student2 = provider.CreateOrUpdate(student);
// var street = lazyStudent.Address.Street;
var student1 = provider.GetById<Student>(1);
var lazyStudent = new LazyStudent(provider, student1);
var address = lazyStudent.Address;
var classes = lazyStudent.Classes;
Console.WriteLine(address.Street);
//student.FirstName = "Marek";
//student.Hobbies.First().Description += " :)"; 
//student.Address.Number = "222";
//provider.CreateOrUpdate(student);
//provider.Remove(student);
//var students = provider.ReadAll<Student>();
//var classes = provider.ReadAll<Class>();
var test = "test";