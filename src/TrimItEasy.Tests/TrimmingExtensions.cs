namespace TrimItEasy.Tests;

public class TrimmingExtensionsTests
{
    [Fact]
    public void Recursive_False()
    {
        // Arrange
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

        // Act
        person.TrimStrings(recursive: false);

        // Assert

        Assert.Equal("John Doe", person.Name);
        Assert.Equal("  123 Elm St  ", person.HomeAddress.Street);
    }

    [Fact]
    public void Recursive_True()
    {
        // Arrange
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

        // Act
        person.TrimStrings(recursive: true);

        // Assert

        Assert.Equal("John Doe", person.Name);
        Assert.Equal("123 Elm St", person.HomeAddress.Street);
    }
}