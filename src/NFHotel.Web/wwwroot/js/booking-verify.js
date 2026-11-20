// The page of the link in the mail of a booking request, /booking/verify (MIL-088, ADR-0052 decision 3).
//
// The token stands after the # of the link, the part of an address that a browser never sends, so no access log
// holds it. This script copies it into the hidden field of the form, takes it out of the address bar and the history,
// and enables the button. Only the button sends the token, in the body of a POST, and only once: the button is disabled
// when it is pressed, and a second submit is stopped. Opening the link changes nothing on the server.
//
// A file of the site's own, loaded by the page: the Content Security Policy refuses inline code (MIL-033).
(function () {
  "use strict";

  const form = document.getElementById("booking-verify-form");
  const field = document.getElementById("booking-verify-token");
  const button = document.getElementById("booking-verify-button");
  const missing = document.getElementById("booking-verify-missing");

  // The answer to a press that did not book shows no form, and there is nothing to do.
  if (!form || !field || !button) {
    return;
  }

  const token = window.location.hash.length > 1 ? window.location.hash.substring(1) : "";

  if (window.location.hash) {
    // Out of the address bar and the entry of the history, so the next person at the screen does not find it there.
    window.history.replaceState(window.history.state, "", window.location.pathname + window.location.search);
  }

  if (!token) {
    if (missing) {
      missing.hidden = false;
    }

    return;
  }

  field.value = token;
  button.disabled = false;

  let isSent = false;
  form.addEventListener("submit", function (event) {
    if (isSent) {
      event.preventDefault();
      return;
    }

    isSent = true;
    button.disabled = true;
  });
}());
