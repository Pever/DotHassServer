﻿using System;

namespace DotHass.Middleware.Mvc.Action.Result
{
    /// <summary>
    /// Represents an <see cref="ActionResult"/> that when executed will
    /// produce an HTTP response with the given response status code.
    /// </summary>
    public class StatusCodeResult : ActionResult, IClientErrorActionResult
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="StatusCodeResult"/> class
        /// with the given <paramref name="statusCode"/>.
        /// </summary>
        /// <param name="statusCode">The HTTP status code of the response.</param>
        public StatusCodeResult(int statusCode)
        {
            StatusCode = statusCode;
        }

        /// <summary>
        /// Gets the HTTP status code.
        /// </summary>
        public int StatusCode { get; }

        int? IStatusCodeActionResult.StatusCode => StatusCode;

        /// <inheritdoc />
        public override void ExecuteResult(ActionContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }
            context.MsgContext.Response.StatusCode = StatusCode;
        }
    }
}