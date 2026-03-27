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
        person.TrimStrings(new TrimmingOptions { Recursive = false });

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
        person.TrimStrings(new TrimmingOptions { Recursive = true });

        // Assert

        Assert.Equal("John Doe", person.Name);
        Assert.Equal("123 Elm St", person.HomeAddress.Street);
    }

    [Fact]
    public void MaxDepth_Zero_TrimsOnlyTopLevelProperties()
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
            }
        };

        // Act
        person.TrimStrings(new TrimmingOptions { MaxDepth = 0 });

        // Assert
        Assert.Equal("John Doe", person.Name);
        Assert.Equal("  123 Elm St  ", person.HomeAddress.Street);
        Assert.Equal("  Springfield  ", person.HomeAddress.City);
        Assert.Equal("  USA  ", person.HomeAddress.CountryInfo.Name);
        Assert.Equal("  Mobile  ", person.Phones[0].Type);
    }

    [Fact]
    public void MaxDepth_One_TrimsOneLevel()
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
            }
        };

        // Act
        person.TrimStrings(new TrimmingOptions { MaxDepth = 1 });

        // Assert
        Assert.Equal("John Doe", person.Name);
        Assert.Equal("123 Elm St", person.HomeAddress.Street);
        Assert.Equal("Springfield", person.HomeAddress.City);
        Assert.Equal("  USA  ", person.HomeAddress.CountryInfo.Name);
        Assert.Equal("  US  ", person.HomeAddress.CountryInfo.Code);
        Assert.Equal("Mobile", person.Phones[0].Type);
        Assert.Equal("123-456-7890", person.Phones[0].Number);
    }

    [Fact]
    public void MaxDepth_Two_TrimsTwoLevels()
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
        };

        // Act
        person.TrimStrings(new TrimmingOptions { MaxDepth = 2 });

        // Assert
        Assert.Equal("John Doe", person.Name);
        Assert.Equal("123 Elm St", person.HomeAddress.Street);
        Assert.Equal("Springfield", person.HomeAddress.City);
        Assert.Equal("USA", person.HomeAddress.CountryInfo.Name);
        Assert.Equal("US", person.HomeAddress.CountryInfo.Code);
    }

    [Fact]
    public void MaxDepth_Default_TrimsAllLevels()
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
        };

        // Act
        person.TrimStrings(new TrimmingOptions());

        // Assert
        Assert.Equal("John Doe", person.Name);
        Assert.Equal("123 Elm St", person.HomeAddress.Street);
        Assert.Equal("Springfield", person.HomeAddress.City);
        Assert.Equal("USA", person.HomeAddress.CountryInfo.Name);
        Assert.Equal("US", person.HomeAddress.CountryInfo.Code);
    }

    [Fact]
    public void MaxDepth_WithRecursiveFalse_DoesNotRecurse()
    {
        // Arrange
        var person = new Person
        {
            Name = "  John Doe  ",
            HomeAddress = new Address
            {
                Street = "  123 Elm St  ",
                City = "  Springfield  ",
            },
        };

        // Act
        person.TrimStrings(new TrimmingOptions { Recursive = false, MaxDepth = 5 });

        // Assert
        Assert.Equal("John Doe", person.Name);
        Assert.Equal("  123 Elm St  ", person.HomeAddress.Street);
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