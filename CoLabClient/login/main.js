var $user = $("input[type=email]"), $pass = $("input[type=password]"), $status = $("#status");

$("form").submit(function (ev) {
    ev.preventDefault();
    $("form").find("input").attr("disabled", true);
    $status.text("Logging in..");
    var login = {
        u: $user.val(),
        p: $pass.val()
    };
    $.ajax({
        url:"http://localhost:5005" + "/login",
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
                if (rp != "")
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