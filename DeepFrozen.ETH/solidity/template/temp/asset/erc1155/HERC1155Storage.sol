// SPDX-License-Identifier: MIT
pragma solidity ^0.8.0;

import "./HERC1155Layout.sol";
import "../erc165/HERC165Storage.sol";
import "../context/ContextStorage.sol";

//this is an endpoint module, only can be directly inherited all the way to the end
contract HERC1155Storage is HERC1155Layout, HERC165Storage, ContextStorage {

    constructor (
        string memory uri_
    )
    HERC165Storage()
    ContextStorage()
    {
        _setURI(uri_);

        // register the supported interfaces to conform to ERC1155 via ERC165
        _registerInterface(_INTERFACE_ID_ERC1155);

        // register the supported interfaces to conform to ERC1155MetadataURI via ERC165
        _registerInterface(_INTERFACE_ID_ERC1155_METADATA_URI);
    }
}
