import {Link} from "react-router-dom";
import "../styles/NavBar.css"
import ReactLogo from "@/assets/react.svg"
import ViteLogo from "@/assets/vite.svg"

function NavBar() {
  return <nav className={"navbar"}>
    <div className={"navbar-brand"}>
      <Link to={"/"}>Climate Dashboard</Link>
    </div>
    <div className={"navbar-links"}>
      <Link to={"/"} className={"nav-link"}>
        <img src={ReactLogo} className={"nav-link-icon"} alt={""}/>
        Home
      </Link>
      <Link to={"/map"} className={"nav-link"}>
        <img src={ViteLogo} className={"nav-link-icon"} alt={""}/>
        Map
      </Link>
    </div>
  </nav>
}

export default NavBar;
