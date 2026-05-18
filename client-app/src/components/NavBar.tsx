import {Link} from "react-router-dom";

function NavBar() {
  return <nav className={"navbar"}>
    <div className={"navbar-brand"}>
      <Link to={"/"}>Climate Dashboard</Link>
    </div>
    <div className={"navbar-links"}>
      <Link to={"/"} className={"nav-link"}>Home</Link>
      <Link to={"/map"} className={"nav-link"}>Map</Link>
    </div>
  </nav>
}

export default NavBar;
