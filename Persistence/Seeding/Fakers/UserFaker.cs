
using Bogus;
using Domain.Models;
using Domain.ValueObjects;

namespace Persistence.Seeding.Fakers;

public static class UserFaker //Provides a static class for generating fake User objects using the Bogus library
{
    public static List<User> Generate(int count) //Generates a list of User objects with the specified count, using the Bogus library to generate random data for the properties of the User class
    {
        var faker = new Faker("en"); //Create a new instance of the Faker class, specifying the locale as "en" (English) to generate data in English format
        faker.Random = new Randomizer(12345); //Set the Random property of the Faker instance to a new Randomizer with a seed value of 12345, ensuring that the generated data is deterministic and reproducible

        var users = new List<User>();

        for (var i = 1; i <= count; i++)
        {
            var firstName = faker.Name.FirstName();
            var lastName = faker.Name.LastName();

            var user = new User(
                username: $"user{i:D4}", //Generate a username in the format "userXXXX", where XXXX is a zero-padded number based on the loop index
                name: new Name(firstName, lastName),
                email: new Email($"user{i:D4}@example.com"),
                dateOfBirth: DateOnly.FromDateTime(
                    faker.Date.Between( //Ensures that no user is below 12 years old.
                        DateTime.Today.AddYears(-70),
                        DateTime.Today.AddYears(-12))),
                address: new Address(
                    faker.Address.StreetName(),
                    faker.Address.BuildingNumber(),
                    faker.Address.ZipCode(),
                    faker.Address.City(),
                    "Denmark")
            );

            users.Add(user);
        }

        return users;
    }
}