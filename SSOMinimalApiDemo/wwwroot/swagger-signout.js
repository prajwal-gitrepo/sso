(function addSignOutButton() {
  function tryAddButton() {
    // Use the correct selector for your Swagger UI version
    var header = document.querySelector('.swagger-ui .topbar-wrapper');
    if (header && !document.querySelector('.swagger-signout-btn')) {
      var btn = document.createElement('a');
      btn.innerText = 'Sign Out';
      btn.href = '/signout';
      btn.style = 'margin-left:20px;font-weight:700;font-size:16px';
      btn.className = 'swagger-signout-btn';
      header.appendChild(btn);
      return true;
    }
    return false;
  }
  // Try every 300ms until the header is found
  var interval = setInterval(function() {
    if (tryAddButton()) clearInterval(interval);
  }, 300);
})();
