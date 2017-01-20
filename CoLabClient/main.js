var Range = require("ace/range").Range;
var editor, session, ws, inserting = false, writing = false;
var $code = $("#code"), $pos = $("#position");

var modelist = ace.require("ace/ext/modelist");
ace.require("ace/ext/language_tools");
var code = $.get("http://localhost:5005/file/test.txt", function (data) {
    $code.text(data);
    editor = ace.edit("code");
    session = editor.getSession();
    editor.$blockScrolling = Infinity;
    session.setMode(modelist.getModeForPath("test.html").mode);
    editor.setOptions({
        enableBasicAutocompletion: true,
        enableSnippets: true,
        enableLiveAutocompletion: true
    });
    editor.setTheme("ace/theme/monokai");
    editor.focus();
    ws = new WebSocket("ws://localhost:5005/test.txt");

    session.selection.on('changeCursor', function() {
        var c = editor.selection.getCursor();
        setPosition(c.row +1, c.column);
    });

    session.on('change', function(e) {
        if (!inserting)
        {
            e.sender = sessionStorage.getItem("id");
            ws.send(JSON.stringify(e));
        }
    });

    ws.onmessage = function (msg) {
        console.log(msg);
        var data = JSON.parse(msg.data);
        if (data.sender == sessionStorage.getItem("id"))
            return;
        console.log(data);
        if (data.start.row >= data.end.row && data.start.column > data.end.column){
            var tem = data.start;
            data.start = data.end;
            data.end = tem;
        }
        inserting = true;
        if (data.action == "insert")
        {
            for (var i = 0; i < data.lines.length; i++){
                editor.session.replace(new Range(data.start.row, data.start.column, data.end.row, data.end.column), data.lines[i]);
            }
        }
        else if (data.action == "remove"){
            for (i = 0; i < data.lines.length; i++){
                editor.session.replace(new Range(data.start.row, data.start.column, data.end.row, data.end.column), "");
            }
        }
        inserting = false;
    };
});

function setPosition(l,c) {
    $pos.text(l + ":" + c);
}

window.addEventListener("beforeunload", function () {
    ws.close();
});

if (sessionStorage.getItem("id") === null){
    sessionStorage.setItem("id", Math.random());
}