﻿using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using System.Web.Http.Results;
using LinqToLdap.Examples.Models;
using LinqToLdap.Examples.Mvc.Models;

namespace LinqToLdap.Examples.Mvc.Controllers.API
{
    public class UsersController : ApiController
    {
        private IDirectoryContext _context;
        public UsersController(IDirectoryContext context)
        {
            _context = context;
        }

        [HttpGet]
        public object Find(string q, bool custom)
        {
            var query = _context.Query<User>();

            if (!string.IsNullOrWhiteSpace(q))
            {
                if (custom)
                {
                    //by default filters passed to the Where method are not cleaned.
                    //if your users don't understand valid filters I would go with fixed search options.
                    query = query.FilterWith(q);
                }
                else
                {
                    var split = q.Split(new[] { " " }, StringSplitOptions.RemoveEmptyEntries);

                    var expression = PredicateBuilder.Create<User>();
                    expression = split.Length == 2
                        ? expression.And(s => s.FirstName.StartsWith(split[0]) && s.LastName.StartsWith(split[1]))
                        : split.Aggregate(expression, (current, t) => current.Or(s => s.UserId == t || s.FirstName.StartsWith(t) || s.LastName.StartsWith(t)));

                    query = query.Where(expression);
                }
            }

            var results = query
                .Select(s =>
                        new UserListItem()
                        {
                            DistinguishedName = s.DistinguishedName,
                            Name = string.Format("{0} {1}", s.FirstName, s.LastName),
                            PrimaryAffiliation = s.PrimaryAffiliation,
                            UserId = s.UserId
                        })
                .Take(50)
                .ToArray();

            return results;
        }

        [HttpGet]
        public object Get(string id)
        {
            var user = _context.Query<User>().FirstOrDefault(u => u.UserId == id);
            
            if (user == null) throw new HttpResponseException(HttpStatusCode.NotFound);

            return user;
        }
    }
}
