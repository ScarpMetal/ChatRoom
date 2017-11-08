var ws = new WebSocket("ws://localhost:5000");

var synth = new Tone.FMSynth().toMaster();

var authKey;
if(sessionStorage.getItem("authKey") === null){
    authKey = "";
} else {
    authKey = sessionStorage.getItem("authKey");
}

$(document).on("click", "#logOutBtn", function () {
    sessionStorage.setItem("authKey", "");
    location.reload();
});

$(document).on("click", "#loginBtn", function () {
    var username = $("#usernameInput").val();
    var password = $("#passwordInput").val();
    sendData("login", [username, password]);
});

$(document).on("click", "#createUserBtn", function () {
    var username = $("#usernameInput").val();
    var password = $("#passwordInput").val();
    sendData("createuser", [username, password]);
});

$(document).on("click", "#notifyBtn", function () {
    $("#notifyList").toggle();
    $("#notifyDot").hide();
});

$(document).on("click", "#sendBtn", function () {
    sendData("message", []);
});

$(document).on("keyup", function (e) {
    if (e.keyCode == 13) {
        if ($("#loginDiv").is(":visible")) {
            //console.log("login is visible")
            var username = $("#usernameInput").val();
            var password = $("#passwordInput").val();
            sendData("login", [username, password]);
        } else if ($("#createRoomDiv").is(":visible")) {
            //console.log("changeroom is visible")
            $("#createRoomDiv").hide();
            var roomName = $("#createRoomNameInput").val();
            sendData("createroom", [roomName]);
            $("#createRoomNameInput").html("");
        } else {
            //console.log("nothing is visible")
            sendData("message", []);
        }    
    }
});

$(document).on("keypress", function (e) {
    if (e.keyCode == 32) {
        if ($("#loginDiv").is(":visible")) {
            return false;
        }
    }
});

$(document).on("click", ".room", function () {
    var roomNumber = $(this).data("roomNumber");
    sendData("changeroom", [roomNumber]);
});

$(document).on("click", "#createRoomBtn", function () {
    $("#createRoomDiv").show();
});

$(document).on("click", "#createRoomCancel", function () {
    $("#createRoomNameInput").html("");
    $("#createRoomDiv").hide();
});

$(document).on("click", "#createRoomSubmitBtn", function () {
    $("#createRoomDiv").hide();
    var roomName = $("#createRoomNameInput").val();
    sendData("createroom", [roomName]);
    $("#createRoomNameInput").html("");
});

function printMessage(data) {
    //console.log(data)
    var messageDiv = $("<div class=message>");
    if (data.highlight == true) {
        messageDiv.css({"background-color":"yellow"})
    }
    var timestamp = $("<span class=timestamp>").html(data.timestamp);
    var username = $("<span class=username>").css({ "color": data.color }).html(" (" + data.username + ") ");
    messageDiv.append(timestamp).append(username).append(data.message);
    $("#messageArea").append(messageDiv);
}

function reloadSidebar(roomData) {
    $("#roomsList").html("");
    for (var key in roomData) {
        var data = roomData[key];
        var room = $("<div class=room>").data("roomNumber", data.roomNumber);
        var accentLine = $("<div class=accentLine>").css({ "background-color": data.color });
        var roomName = $("<div class=roomName>").html(data.name);
        room.append(accentLine).append(roomName);
        $("#roomsList").append(room);
    }
}

function addNotification(data) {
    var str = "<strong>" + data.senderName + "</strong> mentioned you at <strong>" + data.timestamp + "</strong> in <strong>" + data.roomName + "</strong>";
    var notifyItem = $("<div class=notifyItem>").html(str);
    $("#notifyList").append(notifyItem);
    $("#notifyDot").show();
    synth.triggerAttackRelease('C3', '32n');
}

function sendData(type, args) {
    switch (type) {
        case "connected": {
            var data = {
                "type": type,
                "authKey": authKey,
            }
            ws.send(JSON.stringify(data));
            break;
        }
        case "message":
            var message = $("#messageInput").val();
            if (message != "" && message != null) {
                var data = {
                    "type": type,
                    "authKey": authKey,
                    "message": message
                }
                ws.send(JSON.stringify(data));
                $("#messageInput").val("");
            }
            break;
        case "changeroom":
            if (args[0] != "" && args[0] != null) {
                var data = {
                    "type": type,
                    "authKey": authKey,
                    "roomNumber": args[0]
                }
                ws.send(JSON.stringify(data));
                // changeroom not fully implemented yet
            }
            break;
        case "createroom":
            if (args[0] != "" && args[0] != null && args[0].length <= 25) {
                var data = {
                    "type": type,
                    "authKey": authKey,
                    "roomName": args[0]
                }
                ws.send(JSON.stringify(data));
            }
            break;
        case "login":
        case "createuser":
            if (args[0] != "" && args[0] != null && args[1] != "" && args[1] != null) {
                var data = {
                    "type": type,
                    "username": args[0],
                    "password": args[1]
                }
                ws.send(JSON.stringify(data));
            }
            break;
    }
}

ws.onmessage = function (e) {
    var jsonData = JSON.parse(e.data);
    switch (jsonData.type) {
        case "populate":
            $("#messageArea").html("");
            for (var key in jsonData.data) {
                var data = jsonData.data[key];
                printMessage(data);
            }
            $('#messageArea').scrollTop($('#messageArea')[0].scrollHeight);
            break;
        case "message":
            printMessage(jsonData);
            $('#messageArea').scrollTop($('#messageArea')[0].scrollHeight);
            break;
        case "bot":
            var botDiv = $("<div class=bot>").html(jsonData.message);
            $("#messageArea").append(botDiv);
            $('#messageArea').scrollTop($('#messageArea')[0].scrollHeight);
            break;
        case "rooms":
            reloadSidebar(jsonData.data);
            break;
        case "changeroom":
            $("#roomHeader").html(jsonData.roomName);
            $("#accentLine").css({"background-color":jsonData.roomColor});
            break;
        case "authkey":
            if (jsonData.authKey == "") {
                sessionStorage.setItem("authKey", "");
                $("#loginDiv").show();
                $("#master").hide();
                $("#loginErrorMsg").hide();
            } else if (jsonData.authKey == "taken username") {
                $("#loginDiv").show();
                $("#master").hide();
                $("#loginErrorMsg").show().html("taken username");
            } else if (jsonData.authKey == "invalid login") {
                $("#loginDiv").show();
                $("#master").hide();
                $("#loginErrorMsg").show().html("invalid login");
            } else {
                authKey = jsonData.authKey
                sessionStorage.setItem("authKey", authKey);
                $("#loginDiv").hide();
                $("#master").show();
                sendData("connected", []);
            }
            break;
        case "notification":
            addNotification(jsonData);
            break;
        default:
            console.log("Invalid Data Type '" + jsonData.type + "'");
            break;
    }
    
}

ws.onopen = function (e) {
    $("#messageArea").html("");
    $("#roomsList").html("");
    sendData("connected", []);
}