using System.Text.Json;
using System.Text.Json.Serialization;

namespace TrimItEasy.Tests;

public class TrimmingStringJsonConverterTests
{
    private static Person GetPerson()
    {
        var person = new Person
        {
            Name = "  John Doe  ",
            HomeAddress = new Address
            {
                Street = "  123 Elm St  ",
                City = "  Springfield  ",
                CountryInfo = new Country
                {
                    Name = "  USA  ",
                    Code = "  US  "
                }
            },
            Phones = new List<Phone>
            {
                new Phone { Type = "  Mobile  ", Number = " 123-456-7890 " },
                new Phone { Type = " Home ", Number = " 987-654-3210 " }
            }
        };

        var company = new Company
        {
            Name = "  Initech  ",
            OfficeAddress = person.HomeAddress, // shared reference
            Employees = new List<Person> { person }
        };

        person.Employer = company;
        return person;
    }

    [Fact]
    public void ShouldNotTrim()
    {
        // Arrange
        Person? person = GetPerson();

        // Act
        var json = JsonSerializer.Serialize(person, new JsonSerializerOptions
        {
            ReferenceHandler = ReferenceHandler.IgnoreCycles
        });

        person = JsonSerializer.Deserialize<Person>(json);

        // Assert
        Assert.Equal("  John Doe  ", person?.Name);
        Assert.Equal("  123 Elm St  ", person?.HomeAddress.Street);
    }

    [Fact]
    public void Serialize_ShouldTrimRecursively()
    {
        // Arrange
        Person? person = GetPerson();

        // Act
        var json = JsonSerializer.Serialize(person, new JsonSerializerOptions
        {
            ReferenceHandler = ReferenceHandler.IgnoreCycles,
            Converters = { new TrimmingStringJsonConverter() }
        });

        person = JsonSerializer.Deserialize<Person>(json);

        // Assert
        Assert.Equal("John Doe", person?.Name);
        Assert.Equal("123 Elm St", person?.HomeAddress.Street);
    }

    [Fact]
    public void Deserialize_ShouldTrimRecursively()
    {
        // Arrange
        Person? person = GetPerson();

        // Act
        var json = JsonSerializer.Serialize(person, new JsonSerializerOptions
        {
            ReferenceHandler = ReferenceHandler.IgnoreCycles
        });

        person = JsonSerializer.Deserialize<Person>(json, new JsonSerializerOptions
        {
            Converters = { new TrimmingStringJsonConverter() }
        });

        // Assert
        Assert.Equal("John Doe", person?.Name);
        Assert.Equal("123 Elm St", person?.HomeAddress.Street);
    }

    [Fact]
    public void ShouldTrimRecursively()
    {
        // Arrange
        Person? person = GetPerson();

        // Act
        var json = JsonSerializer.Serialize(person, new JsonSerializerOptions
        {
            ReferenceHandler = ReferenceHandler.IgnoreCycles,
            Converters = { new TrimmingStringJsonConverter() }
        });

        person = JsonSerializer.Deserialize<Person>(json, new JsonSerializerOptions
        {
            Converters = { new TrimmingStringJsonConverter() }
        });

        // Assert
        Assert.Equal("John Doe", person?.Name);
        Assert.Equal("123 Elm St", person?.HomeAddress.Street);
    }

    private class Person
    {
        public string Name { get; set; }

        public Address HomeAddress { get; set; }

        public List<Phone> Phones { get; set; }

        public Company Employer { get; set; }
    }

    private class Address
    {
        public string Street { get; set; }

        public string City { get; set; }

        public Country CountryInfo { get; set; }
    }

    private class Country
    {
        public string Name { get; set; }

        public string Code { get; set; }
    }

    private class Phone
    {
        public string Type { get; set; }

        public string Number { get; set; }
    }

    private class Company
    {
        public string Name { get; set; }

        public Address OfficeAddress { get; set; }

        public List<Person> Employees { get; set; }
    }
}
