import './App.css'
import 'maplibre-gl/dist/maplibre-gl.css';
import Solar from './pages/Solar'
import NavBar from './components/NavBar';
import {Route, Routes} from "react-router-dom";
import SolarMap from "./pages/SolarMap.tsx";

function App() {
  return (
    <div className={"app-container"}>
      <NavBar/>
      <main className={"main-content"}>
        <Routes>
          <Route path={"/"} element={<Solar/>}/>
          <Route path={"/map"} element={<SolarMap/>}/>
        </Routes>
      </main>
    </div>
  )
}

export default App
