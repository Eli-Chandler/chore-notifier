import './App.css'
import {Outlet} from "react-router";
import {QueryClient, QueryClientProvider} from "@tanstack/react-query";

const queryClient = new QueryClient()

function App() {
    return (
        <>
            <QueryClientProvider client={queryClient}>
                <Outlet/>
            </QueryClientProvider>
        </>
    )
}

export default App
