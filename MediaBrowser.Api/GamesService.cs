﻿using MediaBrowser.Controller.Dto;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Persistence;
using MediaBrowser.Model.Dto;
using ServiceStack.ServiceHost;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace MediaBrowser.Api
{
    /// <summary>
    /// Class GetSimilarGames
    /// </summary>
    [Route("/Games/{Id}/Similar", "GET")]
    [Api(Description = "Finds games similar to a given game.")]
    public class GetSimilarGames : BaseGetSimilarItemsFromItem
    {
    }

    /// <summary>
    /// Class GetGameSystemSummaries
    /// </summary>
    [Route("/Games/SystemSummaries", "GET")]
    [Api(Description = "Finds games similar to a given game.")]
    public class GetGameSystemSummaries : IReturn<List<GameSystemSummary>>
    {
        /// <summary>
        /// Gets or sets the user id.
        /// </summary>
        /// <value>The user id.</value>
        [ApiMember(Name = "UserId", Description = "Optional. Filter by user id", IsRequired = false, DataType = "string", ParameterType = "query", Verb = "GET")]
        public Guid? UserId { get; set; }
    }

    /// <summary>
    /// Class GamesService
    /// </summary>
    public class GamesService : BaseApiService
    {
        /// <summary>
        /// The _user manager
        /// </summary>
        private readonly IUserManager _userManager;

        /// <summary>
        /// The _user data repository
        /// </summary>
        private readonly IUserDataManager _userDataRepository;
        /// <summary>
        /// The _library manager
        /// </summary>
        private readonly ILibraryManager _libraryManager;

        /// <summary>
        /// The _item repo
        /// </summary>
        private readonly IItemRepository _itemRepo;
        /// <summary>
        /// The _dto service
        /// </summary>
        private readonly IDtoService _dtoService;

        /// <summary>
        /// Initializes a new instance of the <see cref="GamesService" /> class.
        /// </summary>
        /// <param name="userManager">The user manager.</param>
        /// <param name="userDataRepository">The user data repository.</param>
        /// <param name="libraryManager">The library manager.</param>
        /// <param name="itemRepo">The item repo.</param>
        /// <param name="dtoService">The dto service.</param>
        public GamesService(IUserManager userManager, IUserDataManager userDataRepository, ILibraryManager libraryManager, IItemRepository itemRepo, IDtoService dtoService)
        {
            _userManager = userManager;
            _userDataRepository = userDataRepository;
            _libraryManager = libraryManager;
            _itemRepo = itemRepo;
            _dtoService = dtoService;
        }

        /// <summary>
        /// Gets the specified request.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <returns>System.Object.</returns>
        public object Get(GetGameSystemSummaries request)
        {
            var gameSystems = GetAllLibraryItems(request.UserId, _userManager, _libraryManager)
                .OfType<GameSystem>()
                .ToList();

            var user = request.UserId == null ? null : _userManager.GetUserById(request.UserId.Value);

            var result = gameSystems
                .Select(i => GetSummary(i, user))
                .ToList();

            return ToOptimizedResult(result);
        }

        /// <summary>
        /// Gets the summary.
        /// </summary>
        /// <param name="system">The system.</param>
        /// <param name="user">The user.</param>
        /// <returns>GameSystemSummary.</returns>
        private GameSystemSummary GetSummary(GameSystem system, User user)
        {
            var summary = new GameSystemSummary
            {
                Name = system.GameSystemName,
                DisplayName = system.Name
            };

            var items = user == null ? system.RecursiveChildren : system.GetRecursiveChildren(user);

            var games = items.OfType<Game>().ToList();

            summary.ClientInstalledGameCount = games.Count(i => !i.IsInstalledOnClient);

            summary.GameCount = games.Count;

            summary.GameFileExtensions = games.Where(i => !i.IsInstalledOnClient).Select(i => Path.GetExtension(i.Path))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            return summary;
        }

        /// <summary>
        /// Gets the specified request.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <returns>System.Object.</returns>
        public object Get(GetSimilarGames request)
        {
            var result = SimilarItemsHelper.GetSimilarItemsResult(_userManager,
                _itemRepo,
                _libraryManager,
                _userDataRepository,
                _dtoService,
                Logger,
                request, item => item is Game,
                SimilarItemsHelper.GetSimiliarityScore);

            return ToOptimizedResult(result);
        }
    }
}
