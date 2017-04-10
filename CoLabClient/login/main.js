var $user = $("input[type=email]"), $pass = $("input[type=password]"), $status = $("#status");
function getQueryVal(name, url) {
    if (!url)
        url = window.location.href;
    name = name.replace(/[\[\]]/g, "\\$&");
    var regex = new RegExp("[?&]" + name + "(=([^&#]*)|&|#|$)"),
        results = regex.exec(url);
    if (!results) return null;
    if (!results[2]) return '';
    return decodeURIComponent(results[2].replace(/\+/g, " "));
}
$("form").submit(function (ev) {
    ev.preventDefault();
    $("form").find("input").attr("disabled", true);
    $status.text("Logging in..");
    var login = {
        u: $user.val(),
        p: $pass.val()
    };
    $.ajax({
        url:"/login",
        type:"POST",
        data:JSON.stringify(login),
        contentType:"application/json; charset=utf-8",
        success: function(data){
            if (data != "no"){
                var co = JSON.parse(data);
                console.log(co);
                document.cookie = co[0];
                document.cookie = co[1];
                $status.text("");
                var rp = getQueryVal('rp');
                if (rp != null)
                    window.location = rp;
                else
                    window.location = "/projects"
            }
            else
            {
                $status.text("Wrong email or password");
                $("form").find("input").attr("disabled", false);
            }
        }
    });
});