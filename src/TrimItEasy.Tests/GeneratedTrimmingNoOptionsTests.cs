namespace TrimItEasy.Tests;

public class GeneratedTrimmingNoOptionsTests
{
    [Fact]
    public void TrimsAllLevels()
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
            OfficeAddress = person.HomeAddress,
            Employees = new List<Person> { person }
        };

        person.Employer = company;

        // Act
        person.GeneratedTrimStringsNoOptions();

        // Assert
        Assert.Equal("John Doe", person.Name);
        Assert.Equal("123 Elm St", person.HomeAddress.Street);
        Assert.Equal("Springfield", person.HomeAddress.City);
        Assert.Equal("USA", person.HomeAddress.CountryInfo.Name);
        Assert.Equal("US", person.HomeAddress.CountryInfo.Code);
        Assert.Equal("Mobile", person.Phones[0].Type);
        Assert.Equal("123-456-7890", person.Phones[0].Number);
        Assert.Equal("Home", person.Phones[1].Type);
        Assert.Equal("987-654-3210", person.Phones[1].Number);
        Assert.Equal("Initech", person.Employer.Name);
    }

    [Fact]
    public void TrimsNestedObjectProperties()
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
        person.GeneratedTrimStringsNoOptions();

        // Assert
        Assert.Equal("John Doe", person.Name);
        Assert.Equal("123 Elm St", person.HomeAddress.Street);
        Assert.Equal("Springfield", person.HomeAddress.City);
        Assert.Equal("USA", person.HomeAddress.CountryInfo.Name);
        Assert.Equal("US", person.HomeAddress.CountryInfo.Code);
    }

    [Fact]
    public void TrimsCollectionElementProperties()
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
            Phones = new List<Phone>
            {
                new Phone { Type = "  Mobile  ", Number = " 123-456-7890 " },
            }
        };

        // Act
        person.GeneratedTrimStringsNoOptions();

        // Assert
        Assert.Equal("John Doe", person.Name);
        Assert.Equal("Mobile", person.Phones[0].Type);
        Assert.Equal("123-456-7890", person.Phones[0].Number);
    }

    [Fact]
    public void HandlesCircularReferences()
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

        var company = new Company
        {
            Name = "  Initech  ",
            OfficeAddress = person.HomeAddress,
            Employees = new List<Person> { person }
        };

        person.Employer = company;

        // Act
        person.GeneratedTrimStringsNoOptions();

        // Assert
        Assert.Equal("John Doe", person.Name);
        Assert.Equal("Initech", person.Employer.Name);
        Assert.Equal("123 Elm St", person.HomeAddress.Street);
        Assert.Equal("Springfield", person.HomeAddress.City);
    }

    [Fact]
    public void HandlesSharedReferences()
    {
        // Arrange
        var sharedAddress = new Address
        {
            Street = "  123 Elm St  ",
            City = "  Springfield  ",
        };

        var person = new Person
        {
            Name = "  John Doe  ",
            HomeAddress = sharedAddress,
        };

        var company = new Company
        {
            Name = "  Initech  ",
            OfficeAddress = sharedAddress,
            Employees = new List<Person> { person }
        };

        person.Employer = company;

        // Act
        person.GeneratedTrimStringsNoOptions();

        // Assert
        Assert.Equal("123 Elm St", sharedAddress.Street);
        Assert.Equal("Springfield", sharedAddress.City);
        Assert.Same(person.HomeAddress, person.Employer.OfficeAddress);
    }

    [Fact]
    public void HandlesNullProperties()
    {
        // Arrange
        var person = new Person
        {
            Name = "  John Doe  ",
        };

        // Act
        person.GeneratedTrimStringsNoOptions();

        // Assert
        Assert.Equal("John Doe", person.Name);
        Assert.Null(person.HomeAddress);
        Assert.Null(person.Phones);
        Assert.Null(person.Employer);
    }

    [Fact]
    public void HandlesNullStringProperties()
    {
        // Arrange
        var person = new Person
        {
            Name = null,
            HomeAddress = new Address
            {
                Street = null,
                City = "  Springfield  ",
            },
        };

        // Act
        person.GeneratedTrimStringsNoOptions();

        // Assert
        Assert.Null(person.Name);
        Assert.Null(person.HomeAddress.Street);
        Assert.Equal("Springfield", person.HomeAddress.City);
    }
}

public static partial class GeneratedTrimmingNoOptionsPersonExtensions
{
    [GeneratedTrimming]
    public static partial void GeneratedTrimStringsNoOptions(this Person person);
}
