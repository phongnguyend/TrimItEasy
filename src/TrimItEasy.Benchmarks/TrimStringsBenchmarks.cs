using BenchmarkDotNet.Attributes;

namespace TrimItEasy.Benchmarks;

[MemoryDiagnoser]
public class TrimStringsBenchmarks
{
    private Person _person = null!;

    [GlobalSetup]
    public void Setup()
    {
        _person = CreatePerson();
    }

    [IterationSetup]
    public void IterationSetup()
    {
        _person = CreatePerson();
    }

    private static Person CreatePerson()
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
            Phones =
            [
                new Phone { Type = "  Mobile  ", Number = " 123-456-7890 " },
                new Phone { Type = " Home ", Number = " 987-654-3210 " }
            ]
        };

        var company = new Company
        {
            Name = "  Initech  ",
            OfficeAddress = person.HomeAddress,
            Employees = [person]
        };

        person.Employer = company;

        return person;
    }

    [Benchmark(Baseline = true)]
    public void TrimUsingReflection()
    {
        _person.TrimStringsWithReflection();
    }

    [Benchmark]
    public void TrimUsingDelegate()
    {
        _person.TrimStringsWithDelegate();
    }

    [Benchmark]
    public void TrimUsingPartialMethod()
    {
        _person.FastTrimStrings();
    }

    public class Person
    {
        public string Name { get; set; } = null!;
        public Address HomeAddress { get; set; } = null!;
        public List<Phone> Phones { get; set; } = null!;
        public Company Employer { get; set; } = null!;
    }

    public class Address
    {
        public string Street { get; set; } = null!;
        public string City { get; set; } = null!;
        public Country CountryInfo { get; set; } = null!;
    }

    public class Country
    {
        public string Name { get; set; } = null!;
        public string Code { get; set; } = null!;
    }

    public class Phone
    {
        public string Type { get; set; } = null!;
        public string Number { get; set; } = null!;
    }

    public class Company
    {
        public string Name { get; set; } = null!;
        public Address OfficeAddress { get; set; } = null!;
        public List<Person> Employees { get; set; } = null!;
    }
}

public static partial class PersonExtensions
{
    [GeneratedTrimming]
    public static partial void FastTrimStrings(this TrimStringsBenchmarks.Person person);
}