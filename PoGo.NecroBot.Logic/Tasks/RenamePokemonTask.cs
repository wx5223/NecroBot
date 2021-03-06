﻿#region using directives

using System;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using PoGo.NecroBot.Logic.Common;
using PoGo.NecroBot.Logic.Event;
using PoGo.NecroBot.Logic.PoGoUtils;
using PoGo.NecroBot.Logic.State;
using PoGo.NecroBot.Logic.Utils;

#endregion

namespace PoGo.NecroBot.Logic.Tasks
{
    public class RenamePokemonTask
    {
        public static async Task Execute(ISession session, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var pokemons = await session.Inventory.GetPokemons();

            foreach (var pokemon in pokemons)
            {
                cancellationToken.ThrowIfCancellationRequested();

                string perfect = PokemonInfo.CalculatePokemonPerfection(pokemon).ToString("0.00");
                var perfection = Math.Round(PokemonInfo.CalculatePokemonPerfection(pokemon));
                var pokemonName = session.Translation.GetPokemonTranslation(pokemon.PokemonId);
                // iv number + templating part + pokemonName <= 12
                var nameLength = 12 -
                                 (perfect.Length +
                                  session.LogicSettings.RenameTemplate.Length - 6) - 2;
                if (pokemonName.Length > nameLength)
                {
                    pokemonName = pokemonName.Substring(0, nameLength);
                }
                var newNickname = string.Format(session.LogicSettings.RenameTemplate, pokemonName, perfect);
                var oldNickname = pokemon.Nickname.Length != 0 ? pokemon.Nickname : pokemon.PokemonId.ToString();

                // If "RenameOnlyAboveIv" = true only rename pokemon with IV over "KeepMinIvPercentage"
                // Favorites will be skipped
                if ((pokemon.Cp>1000 || perfection >= session.LogicSettings.KeepMinIvPercentage) &&
                    (newNickname != oldNickname && !oldNickname.Contains(newNickname)) && pokemon.Favorite == 0)
                {
                    await session.Client.Inventory.NicknamePokemon(pokemon.Id, newNickname);

                    session.EventDispatcher.Send(new NoticeEvent
                    {
                        Message =
                            session.Translation.GetTranslation(TranslationString.PokemonRename, session.Translation.GetPokemonTranslation(pokemon.PokemonId),
                                pokemon.Id, oldNickname, newNickname)
                    });
                    DelayingUtils.Delay(session.LogicSettings.DelayBetweenPlayerActions, 0);
                }
            }
        }
    }
}