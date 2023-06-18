
using Discord;
using Discord.WebSocket;
using System.Text.RegularExpressions;
using Discord.Net;
using Newtonsoft.Json;

public class Program
{
    public static Task Main(string[] args) => new Program().MainAsync();

    private DiscordSocketClient _client;

    //zapisywanie do konsoli

    private Task Log(LogMessage msg)
    {
        Console.WriteLine(msg.ToString());
        return Task.CompletedTask;
    }

    //połączenie z serwerem i weryfikacja tokenu

    public async Task MainAsync()
    {
        _client = new DiscordSocketClient();

        _client.Log += Log;

        //jeżeli nie działa to znaczy że nie masz tokenu
        var token = File.ReadAllText("token.txt");

        await _client.LoginAsync(TokenType.Bot, token);
        await _client.StartAsync();

        //triggery do podstawowych grup async

        _client.Ready += Client_Ready;
        _client.SlashCommandExecuted += SlashCommandHandler;



        //pętla bez której program by się zamknął

        await Task.Delay(-1);

    }

    //przyjmowanie komend


    public async Task Client_Ready()
    {

        //id serwera testowego

        const ulong guildID = 1097946762838282310u;
        var guild = _client.GetGuild(guildID);

        //podstawowe komendy do sprawdzania czy bot działa

        var poke = new SlashCommandBuilder();
        poke.WithName("poke");
        poke.WithDescription("stab a bot with a pointy stick.");

        var ping = new SlashCommandBuilder();
        ping.WithName("ping");
        ping.WithDescription("pong");

        //kość
        var dice = new SlashCommandBuilder();
        dice.WithName("roll");
        dice.WithDescription("roll the dice.");
        dice.AddOption("dice", ApplicationCommandOptionType.String, "the dice you want to roll.", isRequired: true);


        List<ApplicationCommandProperties> applicationCommandProperties = new();

        //triggery dla komend

        try
        {

            //      kod do komend dostępnych w każdym miejscu gdzie występuje ten bot
            //      await _client.CreateGlobalApplicationCommandAsync(<commandName>.Build());

            //      kod do komend eksluzywnych dla jednego serwera
            //      await guild.CreateApplicationCommandAsync(<commandName>.Build());


            await _client.CreateGlobalApplicationCommandAsync(poke.Build());
            await _client.CreateGlobalApplicationCommandAsync(ping.Build());
            await _client.CreateGlobalApplicationCommandAsync(dice.Build());

        }

        //catch na błędy

        catch (HttpException exception)
        {
            var json = JsonConvert.SerializeObject(exception.Errors, Formatting.Indented);
            Console.WriteLine(json);
        }
    }

    //wykonywanie komend/przekierowywanie ich do swojego taska

    private async Task SlashCommandHandler(SocketSlashCommand command)
    {
        switch (command.Data.Name)
        {
            case "ping":
                await command.RespondAsync("Pong.");
                break;
            case "poke":
                await command.RespondAsync("Bruh.");
                break;
            case "roll":
                await HandleDiceRolls(command);
                break;
        }
    }

    //kod do rzucania kością
    //todo: poprawić komentarze
    private async Task HandleDiceRolls(SocketSlashCommand command)
    {
        string extractedDiceParameters = (string)command.Data.Options.First().Value;

        int sum = 0;
        string resultsSummed;
        var rollList = new List<string>();

        Match diceCountMatch = Regex.Match(extractedDiceParameters, @$"^\d+");
        int diceCount = int.Parse(diceCountMatch.Value);

        Match diceSidesString = Regex.Match(extractedDiceParameters, @$"(?<=d)\d+");
        int diceSides = int.Parse(diceSidesString.Value);

        Match diceModifiersString = Regex.Match(extractedDiceParameters, @$"[+-]\d+$");
        
        Random diceOutput = new Random();
        
        for (int i = 0; i < diceCount; i++)
        {
            int roll = diceOutput.Next(1, diceSides + 1);
            rollList.Add(roll.ToString());
            sum += roll;
        }

        resultsSummed = string.Join(" , ", rollList);

        if (diceModifiersString.Success)
        {
            int diceModifiers = int.Parse(diceModifiersString.Value);
            sum += diceModifiers;
            string diceMod = (diceModifiers > 0 ? "+" : "") + diceModifiers.ToString();
            await command.RespondAsync($"Rolling {diceCount}d{diceSides}{diceMod}.\nYour dices: ({resultsSummed}) result: {sum}");
        }
        else
        {
            await command.RespondAsync($"Rolling {diceCount}d{diceSides}.\nYour dices: ({resultsSummed}) result: {sum}");
        }
    }
}