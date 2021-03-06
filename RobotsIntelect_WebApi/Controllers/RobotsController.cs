﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mime;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Bson;
using RobotsIntelect_WebApi.Models;
using RobotsIntelect_WebApi.Repository.Interfaces;


namespace RobotsIntelect_WebApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class RobotsController : ControllerBase
    {
        private readonly IMongoRepository<Robot> _robotsRepository;

        public RobotsController(IMongoRepository<Robot> robotsRepository)
        {
            _robotsRepository = robotsRepository;
        }


        /// <summary>
        /// Gets all robots.
        /// All user can access this endpoint.
        /// </summary>
        /// <returns></returns>
        [AllowAnnonymous]
        [HttpGet]
        [Produces(MediaTypeNames.Application.Json)]
        [ProducesResponseType(typeof(IEnumerable<Robot>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        public IActionResult GetList()
        {
            var robots = _robotsRepository.AsQueryable().ToList();

            if (robots.Count() > 0)
            {
                return Ok(robots);
            }

            return NoContent();
        }


        /// <summary>
        /// Get robot by id.
        /// All user can access this endpoint.
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [AllowAnnonymous]
        [HttpGet("{id}")]
        [Produces(MediaTypeNames.Application.Json)]
        [ProducesResponseType(typeof(Robot), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(string), StatusCodes.Status404NotFound)]
        public IActionResult Get(string id)
        {
            var robot = _robotsRepository.FindById(id);

            if (robot != null)
            {
                return Ok(robot);
            }

            return NotFound("Entry not found");
        }


        /// <summary>
        /// Create robot
        /// </summary>
        /// <param name="robot"></param>
        /// <returns></returns>
        [Authorize(Roles = Role.AdminOrReferee)]
        [HttpPost]
        [Consumes(MediaTypeNames.Application.Json)]
        [Produces(MediaTypeNames.Application.Json)]
        [ProducesResponseType(typeof(Robot), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(string), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public IActionResult Create([FromBody] Robot robot)
        {
            if (robot == null)
            {
                return BadRequest("Empty request body");
            }

            _robotsRepository.InsertOne(robot);

            var path = $"{Request.Path}/{robot.Id.ToString()}";

            return Created(path, robot);
        }


        /// <summary>
        /// Update robot
        /// </summary>
        /// <param name="id"></param>
        /// <param name="robot"></param>
        /// <returns></returns>
        [Authorize(Roles = Role.AdminOrReferee)]
        [HttpPut("{id}")]
        [Consumes(MediaTypeNames.Application.Json)]
        [Produces(MediaTypeNames.Application.Json)]
        [ProducesResponseType(typeof(Robot), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(string), StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public IActionResult Update(string id, [FromBody] Robot robot)
        {
            if (robot == null)
            {
                return BadRequest("Empty request body!");
            }

            if (!_robotsRepository.Exists(id))
            {
                return NotFound("Entry not found");
            }

            robot.Id = new ObjectId(id);
            _robotsRepository.ReplaceOne(robot);

            return Ok(robot);
        }

        /// <summary>
        /// Delete robot by id
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [Authorize(Roles = Role.AdminOrReferee)]
        [HttpDelete("{id}")]
        [Produces(MediaTypeNames.Application.Json)]
        [ProducesResponseType(typeof(Robot), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(string), StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public IActionResult Delete(string id)
        {
            var robot = _robotsRepository.FindById(id);

            if (robot == null)
            {
                return NotFound("Entry not found");
            }

            _robotsRepository.DeleteById(id);

            return Ok(robot);
        }


        // =========== LAP TIMES ===============

        /// <summary>
        /// Get robot's lap times.
        /// All user can access this endpoint.
        /// </summary>
        /// <param name="id">ID of robot</param>
        /// <returns></returns>
        [AllowAnnonymous]
        [HttpGet("{id}/laptimes")]
        [Produces(MediaTypeNames.Application.Json)]
        [ProducesResponseType(typeof(IEnumerable<LapTime>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(typeof(string), StatusCodes.Status404NotFound)]
        public IActionResult GetLaptimesList(string id)
        {
            var robot = _robotsRepository.FindById(id);

            if(robot == null)
            {
                return NotFound("Entry not found");
            }


            if (robot.LapTimes == null || robot.LapTimes.Count < 1)
            {
                return NoContent();
            }


            return Ok(robot.LapTimes);
        }

        /// <summary>
        /// Update roboto laptimes
        /// </summary>
        /// <param name="id">Robot id</param>
        /// <param name="roundId">Round number</param>
        /// <param name="lapTime">Elapsed time for lap</param>
        /// <returns></returns>
        [Authorize(Roles = Role.Sensor)]
        [HttpPut("{id}/laptimes/{roundId:int}")]
        [Consumes(MediaTypeNames.Application.Json)]
        [Produces(MediaTypeNames.Application.Json)]
        [ProducesResponseType(typeof(Robot), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(string), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public IActionResult CaptureLaptime(string id, int roundId, [FromBody] LapTime lapTime)
        {
            LapTime time = lapTime;
            time.RoundNumber = roundId;

            if (time.RoundNumber < 1 || time.TimeElapsedMs < 1 )
            {
                return BadRequest("Bad request body");
            }

            var robot = _robotsRepository.FindById(id);

            if(robot == null)
            {
                return NotFound("Entry not found");
            }


            if(robot.LapTimes == null)
            {
                robot.LapTimes = new List<LapTime>();
            }

            var lastTime = robot.LapTimes.FirstOrDefault(t => t.RoundNumber == roundId);
            if(lastTime != null)
            {
                robot.LapTimes.Remove(lastTime);
            }

            robot.LapTimes.Add(time);


            _robotsRepository.ReplaceOne(robot);


            return Ok(time);
        }


    }
}
