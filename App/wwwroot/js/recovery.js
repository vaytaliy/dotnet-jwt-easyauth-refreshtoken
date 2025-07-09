/* console.log(document.getElementsByTagName("h1")[0].innerText) */

baseAuthUrl = "https://localhost:5001/auth"


form = document.getElementById("recoveryForm")

const onFormSubmit = async (e) => {
    e.preventDefault()
    pw = document.getElementById("password").value
    const headers = new Headers();
    headers.append("Content-Type", "application/json");

    let params = new URLSearchParams(document.location.search);
    console.log("asdasdas")
    console.log(`${baseAuthUrl}/recovery?${params}`)
    const response = await fetch(`${baseAuthUrl}/recovery?${params}`, {
        method: "PATCH",
        headers: headers,
        body: JSON.stringify({ recoveryPassword: pw }),
    })
}

form.addEventListener("submit", onFormSubmit)