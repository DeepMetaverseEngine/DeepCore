// SPDX-License-Identifier: MIT
pragma solidity ^0.8.0;

import "./HERC4610Layout.sol";
import "../erc721/HERC721Storage.sol";

//this is an endpoint module, only can be directly inherited all the way to the end
contract HERC4610Storage is HERC4610Layout, HERC721Storage {

    constructor (
        string memory name_,
        string memory symbol_,
        string memory baseURI_
    )
    HERC721Storage(name_, symbol_, baseURI_)
    {

        // register the supported interfaces to conform to ERC4610 via ERC165
        _registerInterface(_INTERFACE_ID_ERC4610);
    }
}
