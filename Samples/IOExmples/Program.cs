﻿using System;
using System.Threading.Tasks;
using LanguageExt;
using LanguageExt.Common;
using LanguageExt.Effects;
using static LanguageExt.Prelude;

namespace IOExamples;

class Program
{
    static void Main(string[] args)
    {
        folding.Run(new MinimalRT());
    }

    static IO<MinimalRT, Error, Unit> infiniteLoop(int value) =>
        from _ in value % 100000 == 0
                    ? writeLine($"{value}")
                    : Pure(unit)
        from r in tail(infiniteLoop(value + 1))
        select unit;
    
    static IO<MinimalRT, Error, int> recursiveAskForNumber =>
        from n in askForNumber(1)
        from _ in writeLine($"Your number is: {n}")
        select n;
    
    static IO<MinimalRT, Error, int> askForNumber(int attempts) =>
        from _ in writeLine($"Enter a number (attempt number: {attempts})")
        from l in readLine
        from n in tail(int.TryParse(l, out var v)
                            ? Pure(v)
                            : from _ in writeLine("That's not a number!")
                              from r in tail(askForNumber(attempts + 1))
                              select r)
        select n;

    private static IO<MinimalRT, Error, Unit> folding =>
        from n in many(Range(1, 10))
                    .ToIO<MinimalRT, Error>()
                    .Fold(0, (s, x) => s + x)
        from _ in writeLine($"Total {n}")
        select unit;

    static IO<MinimalRT, Error, Unit> retrying =>
        from ix in many(Range(1, 10))
        from _1 in retry(
            from _2 in writeLine($"Enter a number to add to {ix}")
            from nm in readLine.Map(int.Parse)
            from _3 in writeLine($"Number {ix} + {nm} = {ix + nm}")
            select unit)
        from _4 in waitOneSecond
        select unit;
    
    static IO<MinimalRT, Error, Unit> repeating =>
        from stopAt in lift(() => DateTime.Now.AddSeconds(10))
        from _ in writeLine("READ THE TODO AND DO IT") // TODO: Add the repeat functions to IO.Extensions.Repeat and IO.Prelude.Repeat
        select unit;
    
    static readonly IO<MinimalRT, Error, string> readLine =
        lift(() => Console.ReadLine() ?? "");
    
    static IO<MinimalRT, Error, Unit> writeLine(string line) =>
        lift(() =>
        {
            Console.WriteLine(line);
            return unit;
        });
    
    static IO<MinimalRT, Error, Unit> waitOneSecond =>
        liftIO(async _ => {
            await Task.Delay(1000);
            return unit;
        });
    
    static IO<MinimalRT, Error, DateTime> now =>
        lift(() => DateTime.Now);
}
