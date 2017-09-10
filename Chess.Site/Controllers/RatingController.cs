﻿namespace Chess.Site.Controllers
{
    using System.Linq;
    using Dal;
    using Microsoft.AspNetCore.Mvc;
    using Models;
    using Domain;
    using Microsoft.AspNetCore.Mvc.Rendering;

    public class RatingController : Controller
    {
        private readonly SessionFactory sessionFactory;

        public RatingController(SessionFactory sessionFactory)
        {
            this.sessionFactory = sessionFactory;
        }

        [HttpGet]
        public IActionResult Index()
        {
            var viewModel = sessionFactory.Execute(s =>
            {
                var players = GetPlayers(s);

                return new RatingViewModel
                {
                    Players = players.Select(x => new SelectListItem { Text = x.Name, Value = x.Id.ToString() }).ToArray(),
                    Rating = players.OrderByDescending(x => x.Decipoints)
                                    .ThenBy(x => x.Id)
                                    .Select(x => new Rating
                                                 {
                                                     Name = x.Name,
                                                     Points = x.Points
                                                 }
                                    )
                                    .ToArray()
                    ,
                    LatestResults = s.Query<GameResult>("SELECT * FROM gameResults ORDER BY createdAt DESC")
                                     .Select(x => new ResultViewModel
                                                  {
                                                      Winner = x.Winner,
                                                      BlackPlayer = players.Single(p => p.Id == x.BlackPlayerId).Name,
                                                      WhitePlayer = players.Single(p => p.Id == x.WhitePlayerId).Name
                                                  }
                                     )
                                     .ToArray()

                };
            });

            return View(viewModel);
        }

        [HttpPost]
        public IActionResult Index(CreateGameResultDto dto)
        {
            if (dto.BlackPlayerId == dto.WhitePlayerId)
                return RedirectToAction("Index");

            sessionFactory.Execute(s =>
            {
                var whitePlayer = GetPlayerById(s, dto.WhitePlayerId);
                var blackPlayer = GetPlayerById(s, dto.BlackPlayerId);

                var result = new GameResult(whitePlayer, blackPlayer, dto.Winner);

                s.Execute(@"INSERT INTO gameResults(whitePlayerId, blackPlayerId, whiteDeltaDecipoints, blackDeltaDecipoints, winner, createdAt)
                            VALUES(@WhitePlayerId, @BlackPlayerId, @WhiteDeltaDecipoints, @BlackDeltaDecipoints, @Winner, @CreatedAt)", result);

                UpdatePlayerDecipoints(s, whitePlayer);
                UpdatePlayerDecipoints(s, blackPlayer);
            });

            return RedirectToAction("Index");
        }

        private void UpdatePlayerDecipoints(Session session, Player player)
        {
            session.Execute("UPDATE players SET decipoints=@Decipoints WHERE id=@Id", player);
        }

        private Player GetPlayerById(Session session, int playerId)
        {
            return session.Query<Player>("SELECT * FROM players WHERE id=@playerId", new { playerId }).Single();
        }

        private Player[] GetPlayers(Session session)
        {
            return session.Query<Player>("SELECT * FROM players ORDER BY id");
        }

        [HttpGet]
        public IActionResult Players()
        {
            var players = sessionFactory.Execute(s => GetPlayers(s));

            return View(new PlayersViewModel
            {
                Players = players
            });
        }

        [HttpPost]
        public IActionResult Players(Player dto)
        {
            if (string.IsNullOrEmpty(dto.Name))
                return RedirectToAction("Players");

            sessionFactory.Execute(s =>
            {
                s.Execute("INSERT INTO players(name, slackNickname, decipoints) VALUES(@Name, @SlackNickname, @Decipoints)",
                    new {dto.Name, dto.SlackNickname, Decipoints = GameResult.StartDecipoints});
            });

            return RedirectToAction("Players");
        }
    }
}