// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.
"use strict";


var connection = new signalR.HubConnectionBuilder().withUrl("/hub").build();
connection.on("EndpointStatusUpdatedAsync", function (key, health, readiness, liveness) {
    console.log("EndpointStatusUpdatedAsync('" + key + "', '" + health + "', '" + readiness + "', '" + liveness + "')");
    $("#endpoint-" + key + " td.endpoint-health").html(health);
    $("#endpoint-" + key + " td.endpoint-readiness").html(readiness);
    $("#endpoint-" + key + " td.endpoint-liveness").html(liveness);
});

connection.on("EndpointMetricsUpdatedAsync", function (key, metrics) {
    console.log("EndpointMetricsUpdatedAsync('" + key + "'):");
    console.log(metrics);
    $("#endpoint-metrics-" + key).html(metrics);
});

connection.start().then(function () {
    console.log("SignalR Hub connected")
}).catch(function (err) {
    return console.error(err.toString());
});