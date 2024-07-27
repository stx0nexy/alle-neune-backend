using System.Text;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Reserve.API.Model;

namespace Reserve.API.Apis;

public static class ReserveApi
{
    public static RouteGroupBuilder MapReserveApiV1(this IEndpointRouteBuilder app)
    {
        var api = app.MapGroup("api/reserve").HasApiVersion(1.0);

        // Routes for querying catalog items.
        api.MapGet("/reserves", GetAllItems);
        api.MapGet("/reserves/by", GetItemsByIds);
        api.MapGet("/reserves/{id:int}", GetItemById);
        api.MapGet("/notreserves", GetNotAvailableDateTime).AllowAnonymous();


        // Routes for modifying catalog items.
        api.MapPut("/items", UpdateItem);
        api.MapPost("/items", CreateItem).AllowAnonymous();
        api.MapDelete("/items/{id:int}", DeleteItemById);
        api.MapPost("/reservenotavailable", CreateNotAvailableDateTime);

        return api;
    }
    
    public static async Task<Results<Ok<PaginatedItems<ReserveItem>>, BadRequest<string>>> GetAllItems(
        [AsParameters] ReserveServices services)
    {
        var totalItems = await services.Context.ReserveItems.LongCountAsync();

        var itemsOnPage = await services.Context.ReserveItems
            .OrderBy(c => c.DateReservation)
            .ToListAsync();

        foreach (var item in itemsOnPage)
        {
            item.Name = Encoding.UTF8.GetString(await services.EncryptionService.DecryptAsync("customer-data-key", Convert.FromBase64String(item.Name)));
            item.Surname = Encoding.UTF8.GetString(await services.EncryptionService.DecryptAsync("customer-data-key", Convert.FromBase64String(item.Surname)));
            item.PhoneNumber = Encoding.UTF8.GetString(await services.EncryptionService.DecryptAsync("customer-data-key", Convert.FromBase64String(item.PhoneNumber)));
        }

        return TypedResults.Ok(new PaginatedItems<ReserveItem>(totalItems, itemsOnPage));
    }
    
    public static async Task<Ok<List<ReserveItem>>> GetItemsByIds(
        [AsParameters] ReserveServices services,
        int[] ids)
    {
        var items = await services.Context.ReserveItems
            .Where(item => ids.Contains(item.Id))
            .ToListAsync();

        foreach (var item in items)
        {
            item.Name = Encoding.UTF8.GetString(await services.EncryptionService.DecryptAsync("customer-data-key", Convert.FromBase64String(item.Name)));
            item.Surname = Encoding.UTF8.GetString(await services.EncryptionService.DecryptAsync("customer-data-key", Convert.FromBase64String(item.Surname)));
            item.PhoneNumber = Encoding.UTF8.GetString(await services.EncryptionService.DecryptAsync("customer-data-key", Convert.FromBase64String(item.PhoneNumber)));
        }

        return TypedResults.Ok(items);
    }

    
    public static async Task<Results<Ok<ReserveItem>, NotFound, BadRequest<string>>> GetItemById(
        [AsParameters] ReserveServices services,
        int id)
    {
        if (id <= 0)
        {
            return TypedResults.BadRequest("Id is not valid.");
        }

        var item = await services.Context.ReserveItems
            .SingleOrDefaultAsync(ci => ci.Id == id);

        if (item == null)
        {
            return TypedResults.NotFound();
        }

        item.Name = Encoding.UTF8.GetString(await services.EncryptionService.DecryptAsync("customer-data-key", Convert.FromBase64String(item.Name)));
        item.Surname = Encoding.UTF8.GetString(await services.EncryptionService.DecryptAsync("customer-data-key", Convert.FromBase64String(item.Surname)));
        item.PhoneNumber = Encoding.UTF8.GetString(await services.EncryptionService.DecryptAsync("customer-data-key", Convert.FromBase64String(item.PhoneNumber)));

        return TypedResults.Ok(item);
    }


    public static async Task<Results<Ok<List<DateTimeNotAvailable>>, BadRequest<string>>> GetNotAvailableDateTime(
        [AsParameters] ReserveServices services)
    {
        var reservations = await services.Context.Set<ReserveItem>().ToListAsync();

        var timeSlots = new List<TimeSlot>();

        foreach (var reservation in reservations)
        {
            var date = reservation.DateReservation;
            var time = reservation.TimeReservation;
            var dateTime = date.Add(time);

            var startTime = dateTime;
            var endTime = dateTime.AddHours(1.5);

            timeSlots.Add(new TimeSlot
            {
                Start = startTime,
                End = endTime,
                Count = reservation.CountPersons
            });

            Console.WriteLine($"Added time slot: Start={startTime}, End={endTime}, Count={reservation.CountPersons}");
        }

        var allTimes = timeSlots.SelectMany(t => new[] { t.Start, t.End }).Distinct().OrderBy(t => t).ToList();

        Console.WriteLine($"All times: {string.Join(", ", allTimes.Select(t => t.ToString()))}");

        var busyPeriods = new List<DateTimeNotAvailable>();

        for (int i = 0; i < allTimes.Count - 1; i++)
        {
            var start = allTimes[i];
            var end = allTimes[i + 1];

            var count = timeSlots.Where(t => t.Start < end && t.End > start)
                                 .Sum(t => t.Count);

            Console.WriteLine($"Checking interval: Start={start}, End={end}, Total Count={count}");

            if (count >= 30)
            {
                busyPeriods.Add(new DateTimeNotAvailable
                {
                    Date = start.Date,
                    StartTime = start.TimeOfDay,
                    EndTime = end.TimeOfDay
                });
            }
        }

        

        foreach (var period in busyPeriods)
        {
            await services.Context.Set<DateTimeNotAvailable>().AddAsync(period);
        }
        await services.Context.SaveChangesAsync();
        var result = await services.Context.Set<DateTimeNotAvailable>().ToListAsync();
        if (result.Count == 0)
        {
            return TypedResults.BadRequest("No busy periods found.");
        }

        return TypedResults.Ok(result);
    }
    
    public static async Task<Results<Created, NotFound<string>>> UpdateItem(
        [AsParameters] ReserveServices services,
        ReserveItem itemToUpdate)
    {
        var catalogItem = await services.Context.ReserveItems.SingleOrDefaultAsync(i => i.Id == itemToUpdate.Id);

        if (catalogItem == null)
        {
            return TypedResults.NotFound($"Item with id {itemToUpdate.Id} not found.");
        }

        // Update current product
        var catalogEntry = services.Context.Entry(catalogItem);
        catalogEntry.CurrentValues.SetValues(itemToUpdate);

        //catalogItem.Embedding = await services.CatalogAI.GetEmbeddingAsync(catalogItem);
        
        await services.Context.SaveChangesAsync();
        
        return TypedResults.Created($"/api/reserve/items/{itemToUpdate.Id}");
    }

       public static async Task<Results<Created, NotFound<string>>> CreateItem(
    [AsParameters] ReserveServices services,
    ReserveItem reserveItem)
    {
        var validationErrors = ValidateReserveItem(reserveItem);
        if (validationErrors.Any())
        {
            return TypedResults.NotFound(string.Join(", ", validationErrors));
        }

        //var date = reserveItem.DateReservation;
        var timeString = reserveItem.TimeReservation.ToString();
        var time = TimeSpan.Parse(timeString);

        //var dateTime = date.Add(time);

        //var startTimeRange = dateTime.TimeOfDay;
        //var endTimeRange = dateTime.AddHours(1.5).TimeOfDay;

        //var totalPersonsInRange = await services.Context.ReserveItems
            //.Where(r => r.DateReservation == date && r.TimeReservation >= startTimeRange && r.TimeReservation <= endTimeRange)
            //.SumAsync(r => r.CountPersons);

        //if (totalPersonsInRange + reserveItem.CountPersons > 30)
        //{
            //return TypedResults.NotFound("Превышено ограничение в 30 человек в одно время.");
        //}
        
        var encryptedName = await services.EncryptionService.EncryptAsync("customer-data-key", Encoding.UTF8.GetBytes(reserveItem.Name));
        var encryptedSurname = await services.EncryptionService.EncryptAsync("customer-data-key", Encoding.UTF8.GetBytes(reserveItem.Surname));
        var encryptedPhoneNumber = await services.EncryptionService.EncryptAsync("customer-data-key", Encoding.UTF8.GetBytes(reserveItem.PhoneNumber));
        
        var item = new ReserveItem()
        {
            Game = reserveItem.Game,
            DateReservation = reserveItem.DateReservation,
            TimeReservation = time,
            EatAndPlay = reserveItem.EatAndPlay,
            Name = Convert.ToBase64String(encryptedName),
            Surname = Convert.ToBase64String(encryptedSurname),
            PhoneNumber = Convert.ToBase64String(encryptedPhoneNumber),
            CountPersons = reserveItem.CountPersons,
            Message = reserveItem.Message
        };

        services.Context.ReserveItems.Add(item);
        await services.Context.SaveChangesAsync();
        /*string subject = "Neue Reservierung";
        string body = $@"
        <h2>Neue Reservierung</h2>
        <p><strong>Datum der Reservierung:</strong> {reserveItem.DateReservation:dd.MM.yyyy}</p>
        <p><strong>Uhrzeit der Reservierung:</strong> {reserveItem.TimeReservation}</p>
        <p><strong>Name:</strong> {reserveItem.Name}</p>
        <p><strong>Nachname:</strong> {reserveItem.Surname}</p>
        <p><strong>Telefonnummer:</strong> {reserveItem.PhoneNumber}</p>
        <p><strong>Anzahl der Personen:</strong> {reserveItem.CountPersons}</p>
        <p><strong>Nachricht:</strong> {reserveItem.Message}</p>
    ";
        // Отправка письма
        await services.EmailService.SendEmailAsync("sofiia.nesterenko@nure.ua", subject, body);*/

        return TypedResults.Created($"/api/reserve/items/{reserveItem.Id}");
    }


    public static async Task<Results<NoContent, NotFound>> DeleteItemById(
        [AsParameters] ReserveServices services,
        int id)
    {
        var item = services.Context.ReserveItems.SingleOrDefault(x => x.Id == id);

        if (item is null)
        {
            return TypedResults.NotFound();
        }

        services.Context.ReserveItems.Remove(item);
        await services.Context.SaveChangesAsync();
        return TypedResults.NoContent();
    }

    public static async Task<Results<Created, NotFound<string>>> CreateNotAvailableDateTime(
        [AsParameters] ReserveServices services,
        DateTimeNotAvailable reserveItem)
    {
        var item = new DateTimeNotAvailable
        {
            Date = reserveItem.Date.ToUniversalTime(),
            StartTime = reserveItem.StartTime,
            EndTime = reserveItem.EndTime
        };

        services.Context.DateTimeNotAvailables.Add(item);
        await services.Context.SaveChangesAsync();

        return TypedResults.Created($"/api/reservenotavailable/items/{item.Id}");
    }
    private class TimeSlot
    {
        public DateTime Start { get; set; }
        public DateTime End { get; set; }
        public int Count { get; set; }
    }
    
    private static IEnumerable<string> ValidateReserveItem(ReserveItem item)
    {
        var errors = new List<string>();

        if (item.DateReservation == default)
            errors.Add("Datum ist erforderlich.");

        if (item.TimeReservation == default)
            errors.Add("Uhrzeit ist erforderlich.");

        if (string.IsNullOrWhiteSpace(item.Name) || item.Name.Length > 200)
            errors.Add("Name ist erforderlich und darf nicht länger als 200 Zeichen sein.");

        if (string.IsNullOrWhiteSpace(item.Surname) || item.Surname.Length > 200)
            errors.Add("Nachname ist erforderlich und darf nicht länger als 200 Zeichen sein.");

        if (!Regex.IsMatch(item.PhoneNumber, @"^\+49\d{10,11}$"))
            errors.Add("Telefonnummer muss mit +49 beginnen und 12 bis 13 Ziffern enthalten.");

        if (item.CountPersons < 1 || item.CountPersons > 6)
            errors.Add("Anzahl der Personen muss zwischen 1 und 6 liegen.");

        if (item.Message?.Length > 500)
            errors.Add("Nachricht darf nicht länger als 500 Zeichen sein.");

        return errors;
    }
}